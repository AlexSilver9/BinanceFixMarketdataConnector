using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;

namespace BinanceFixMarketdataConnector;

/// <summary>
/// Processes market data messages (snapshot/incremental/reject), maintains in-memory cache by symbol
/// </summary>
public class MarketDataHandler(ILogger<MarketDataHandler> logger)
{
    private readonly Dictionary<string, MarketDataSnapshot> _marketDataCache = new();

    public event EventHandler<MarketDataSnapshot>? OnMarketDataReceived;

    public void ProcessMessage(Message message)
    {
        var msgType = message.Header.GetString(Tags.MsgType);
        switch (msgType)
        {
            case var _ when msgType == FixConstants.MsgTypes.MarketDataSnapshot:
                HandleMarketDataSnapshot(message);
                break;
            case var _ when msgType == FixConstants.MsgTypes.MarketDataIncremental:
                HandleMarketDataIncremental(message);
                break;
            case var _ when msgType == FixConstants.MsgTypes.MarketDataReject:
                HandleMarketDataReject(message);
                break;
        }
    }

    public MarketDataSnapshot? GetLatestSnapshot(string symbol) =>
        _marketDataCache.GetValueOrDefault(symbol);

    private void HandleMarketDataSnapshot(Message message)
    {
        var symbol = message.GetString(Tags.Symbol);
        var snapshot = new MarketDataSnapshot { Symbol = symbol, Timestamp = DateTime.UtcNow };

        if (!message.IsSetField(Tags.NoMDEntries)) return;

        for (var i = 1; i <= message.GetInt(Tags.NoMDEntries); i++)
        {
            var group = new Group(Tags.NoMDEntries, Tags.MDEntryType);
            message.GetGroup(i, group);
            UpdateSnapshot(snapshot, group.GetChar(Tags.MDEntryType),
                          group.GetDecimal(Tags.MDEntryPx),
                          group.GetDecimal(Tags.MDEntrySize));
        }

        _marketDataCache[symbol] = snapshot;
        logger.LogDebug("Snapshot: {Symbol}", symbol);
        OnMarketDataReceived?.Invoke(this, snapshot);
    }

    private void HandleMarketDataIncremental(Message message)
    {
        if (!message.IsSetField(Tags.NoMDEntries)) return;

        for (var i = 1; i <= message.GetInt(Tags.NoMDEntries); i++)
        {
            var group = new Group(Tags.NoMDEntries, Tags.MDUpdateAction);
            message.GetGroup(i, group);

            // Symbol may be in group or in message header
            var symbol = group.IsSetField(Tags.Symbol)
                ? group.GetString(Tags.Symbol)
                : message.IsSetField(Tags.Symbol)
                    ? message.GetString(Tags.Symbol)
                    : null;

            if (symbol == null) continue;

            var action = group.GetChar(Tags.MDUpdateAction);
            if (action != MDUpdateAction.NEW && action != MDUpdateAction.CHANGE) continue;

            if (!_marketDataCache.TryGetValue(symbol, out var snapshot))
            {
                snapshot = new MarketDataSnapshot { Symbol = symbol };
                _marketDataCache[symbol] = snapshot;
            }

            UpdateSnapshot(snapshot, group.GetChar(Tags.MDEntryType),
                          group.GetDecimal(Tags.MDEntryPx),
                          group.GetDecimal(Tags.MDEntrySize));
            snapshot.Timestamp = DateTime.UtcNow;
            OnMarketDataReceived?.Invoke(this, snapshot);
        }
    }

    private static void UpdateSnapshot(MarketDataSnapshot snapshot, char entryType, decimal price, decimal size)
    {
        switch (entryType)
        {
            case MDEntryType.BID:
                snapshot.BidPrice = price;
                snapshot.BidSize = size;
                break;
            case MDEntryType.OFFER:
                snapshot.AskPrice = price;
                snapshot.AskSize = size;
                break;
            case MDEntryType.TRADE:
                snapshot.LastPrice = price;
                snapshot.LastSize = size;
                break;
        }
    }

    private void HandleMarketDataReject(Message message) =>
        logger.LogWarning("Market Data Request Rejected - ID: {MdReqId}, Reason: {RejectReason}",
            message.GetString(Tags.MDReqID),
            message.IsSetField(Tags.MDReqRejReason) ? message.GetString(Tags.MDReqRejReason) : "Unknown");
}
