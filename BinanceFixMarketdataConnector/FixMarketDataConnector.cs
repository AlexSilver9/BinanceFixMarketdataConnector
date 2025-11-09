using QuickFix;
using QuickFix.Fields;
using Microsoft.Extensions.Logging;

namespace BinanceFixMarketdataConnector;

/// <summary>
/// High-level connector for Binance FIX Market Data API
/// </summary>
public class FixMarketDataConnector
{
    private readonly FixApplicationCallbacks _callbacks;
    private readonly MarketDataHandler _marketDataHandler;
    private Session? _session;

    public FixMarketDataConnector(string apiKey, string privateKeyPath, ILoggerFactory loggerFactory)
    {
        var authHandler = new Ed25519AuthenticationHandler(apiKey, privateKeyPath);
        _marketDataHandler = new MarketDataHandler(loggerFactory.CreateLogger<MarketDataHandler>());
        _callbacks = new FixApplicationCallbacks(authHandler, _marketDataHandler, loggerFactory.CreateLogger<FixApplicationCallbacks>());

        // Wire up events
        _callbacks.OnStatusChanged += (sender, status) =>
        {
            if (status == "Session Created" && _callbacks.SessionId != null)
                _session = Session.LookupSession(_callbacks.SessionId);
            OnStatusChanged?.Invoke(this, status);
        };

        _marketDataHandler.OnMarketDataReceived += (sender, snapshot) =>
            OnMarketDataReceived?.Invoke(this, snapshot);
    }

    public IApplication Application => _callbacks;

    public event EventHandler<MarketDataSnapshot>? OnMarketDataReceived;
    public event EventHandler<string>? OnStatusChanged;

    public void SubscribeMarketData(string symbol, string mdReqId)
    {
        if (_session == null)
        {
            throw new InvalidOperationException("Session not established. Cannot subscribe.");
        }

        var request = new Message();
        request.Header.SetField(new MsgType(FixConstants.MsgTypes.MarketDataRequest));
        request.SetField(new MDReqID(mdReqId));
        request.SetField(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));
        request.SetField(new MarketDepth(1));

        request.SetField(new NoRelatedSym(1));
        var symbolGroup = new Group(Tags.NoRelatedSym, Tags.Symbol);
        symbolGroup.SetField(new Symbol(symbol));
        request.AddGroup(symbolGroup);

        request.SetField(new NoMDEntryTypes(3));
        foreach (var entryType in new[] { MDEntryType.BID, MDEntryType.OFFER, MDEntryType.TRADE })
        {
            var group = new Group(Tags.NoMDEntryTypes, Tags.MDEntryType);
            group.SetField(new MDEntryType(entryType));
            request.AddGroup(group);
        }

        Session.SendToTarget(request, _session.SessionID);
    }

    public void UnsubscribeMarketData(string mdReqId)
    {
        if (_session == null) return;

        var request = new Message();
        request.Header.SetField(new MsgType(FixConstants.MsgTypes.MarketDataRequest));
        request.SetField(new MDReqID(mdReqId));
        request.SetField(new SubscriptionRequestType(SubscriptionRequestType.DISABLE_PREVIOUS_SNAPSHOT_PLUS_UPDATE_REQUEST));
        request.SetField(new MarketDepth(1));

        Session.SendToTarget(request, _session.SessionID);
    }

    public MarketDataSnapshot? GetLatestSnapshot(string symbol) =>
        _marketDataHandler.GetLatestSnapshot(symbol);

    public void Logout()
    {
        if (_session == null) return;
        _session.Logout("User requested disconnect");
    }
}

public class MarketDataSnapshot
{
    public required string Symbol { get; init; }
    public decimal BidPrice { get; set; }
    public decimal BidSize { get; set; }
    public decimal AskPrice { get; set; }
    public decimal AskSize { get; set; }
    public decimal LastPrice { get; set; }
    public decimal LastSize { get; set; }
    public DateTime Timestamp { get; set; }
}
