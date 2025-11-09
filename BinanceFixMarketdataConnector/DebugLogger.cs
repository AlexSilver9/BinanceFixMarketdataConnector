using QuickFix;
using QuickFix.Logger;
using Microsoft.Extensions.Logging;

namespace BinanceFixMarketdataConnector;

public class QuickFixLogFactory(ILoggerFactory loggerFactory) : ILogFactory
{
    public ILog Create(SessionID sessionId) =>
        new QuickFixLog(loggerFactory.CreateLogger($"QuickFIX.{sessionId}"));

    public ILog CreateNonSessionLog() =>
        new QuickFixLog(loggerFactory.CreateLogger("QuickFIX"));
}

public class QuickFixLog(ILogger logger) : ILog
{
    public void OnIncoming(string msg) => logger.LogDebug("INCOMING: {Message}", msg);
    public void OnOutgoing(string msg) => logger.LogDebug("OUTGOING: {Message}", msg);
    public void OnEvent(string text) => logger.LogDebug("EVENT: {Text}", text);
    public void Clear() { }
    public void Dispose() => GC.SuppressFinalize(this);
}
