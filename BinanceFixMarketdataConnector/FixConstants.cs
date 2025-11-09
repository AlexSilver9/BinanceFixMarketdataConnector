namespace BinanceFixMarketdataConnector;

/// <summary>FIX protocol constants for Binance market data API</summary>
public static class FixConstants
{
    public static class MsgTypes
    {
        public const string Logon = "A";
        public const string Reject = "3";
        public const string Logout = "5";
        public const string MarketDataRequest = "V";
        public const string MarketDataSnapshot = "W";
        public const string MarketDataIncremental = "X";
        public const string MarketDataReject = "Y";
    }

    public static class BinanceFields
    {
        public const int RecvWindow = 25000;
        public const int MessageHandling = 25035;
    }

    public static class MessageHandlingValues
    {
        public const int Unordered = 1;
        public const int Sequential = 2;
    }

    public const char SOH = '\x01';
}
