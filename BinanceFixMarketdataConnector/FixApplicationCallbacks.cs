using System.Text;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Fields;

namespace BinanceFixMarketdataConnector;

/// <summary>QuickFIX IApplication implementation delegating to authentication and market data handlers</summary>
public class FixApplicationCallbacks(
    Ed25519AuthenticationHandler authHandler,
    MarketDataHandler marketDataHandler,
    ILogger<FixApplicationCallbacks> logger) : IApplication
{
    public event EventHandler<string>? OnStatusChanged;
    public SessionID? SessionId { get; private set; }

    public void OnCreate(SessionID sessionId)
    {
        SessionId = sessionId;
        logger.LogDebug("Session created: {SessionID}", sessionId);
        OnStatusChanged?.Invoke(this, "Session Created");
    }

    public void OnLogon(SessionID sessionId)
    {
        logger.LogInformation("Logon successful: {SessionID}", sessionId);
        OnStatusChanged?.Invoke(this, "Logged In");
    }

    public void OnLogout(SessionID sessionId)
    {
        logger.LogInformation("Logout: {SessionID}", sessionId);
        OnStatusChanged?.Invoke(this, "Logged Out");
    }

    public void ToAdmin(Message message, SessionID sessionId)
    {
        logger.LogDebug("ToAdmin message: {Message}", message);

        var msgType = message.Header.GetString(Tags.MsgType);
        if (msgType == FixConstants.MsgTypes.Logon)
        {
            authHandler.PrepareAndSignLogonMessage(message);
            logger.LogDebug("Logon prepared with Ed25519 signature");
        }
    }

    public void FromAdmin(Message message, SessionID sessionId)
    {
        var msgType = message.Header.GetString(Tags.MsgType);
        logger.LogDebug("FromAdmin: {MsgType}", msgType);

        switch (msgType)
        {
            case var _ when msgType == FixConstants.MsgTypes.Logon:
                logger.LogDebug("Logon ack received");
                break;
            case var _ when msgType == FixConstants.MsgTypes.Reject:
                var details = new StringBuilder("Message REJECTED");
                if (message.IsSetField(Tags.RefSeqNum))
                    details.Append($", RefSeqNum={message.GetInt(Tags.RefSeqNum)}");
                if (message.IsSetField(Tags.RefMsgType))
                    details.Append($", RefMsgType={message.GetString(Tags.RefMsgType)}");
                if (message.IsSetField(Tags.SessionRejectReason))
                    details.Append($", Reason={message.GetInt(Tags.SessionRejectReason)}");
                if (message.IsSetField(Tags.Text))
                    details.Append($", Text={message.GetString(Tags.Text)}");
                logger.LogError("{Details}", details.ToString());
                OnStatusChanged?.Invoke(this, "Message Rejected");
                break;
            case var _ when msgType == FixConstants.MsgTypes.Logout:
                var reason = message.IsSetField(Tags.Text) ? message.GetString(Tags.Text) : "";
                logger.LogWarning("LOGOUT received{Reason}", reason != "" ? $": {reason}" : "");
                OnStatusChanged?.Invoke(this, reason != "" ? $"Logged Out - {reason}" : "Logged Out");
                break;
        }
    }

    public void ToApp(Message message, SessionID sessionId) =>
        logger.LogDebug("ToApp: {Message}", message);

    public void FromApp(Message message, SessionID sessionId)
    {
        logger.LogDebug("FromApp: {MessageType}", message.GetType().Name);

        try
        {
            marketDataHandler.ProcessMessage(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message");
        }
    }
}
