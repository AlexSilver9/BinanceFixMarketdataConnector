using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Store;
using QuickFix.Transport;
using BinanceFixMarketdataConnector;

// Load configuration
var config = ApplicationConfiguration.Load();
config.Validate();

// Setup logging
using var loggerFactory = LoggingConfiguration.CreateLoggerFactory(config.Logging.LogLevel);
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("FIX 4.4 Market Data Connector");
logger.LogInformation("Press 'Q' to quit");
logger.LogInformation("Press 'S' to show latest snapshots");
logger.LogInformation("Log Level: {LogLevel}", config.Logging.LogLevel);
logger.LogInformation("==============================");

try
{
    // Create the FIX connector
    var connector = new FixMarketDataConnector(config.Binance.ApiKey, config.Binance.PrivateKeyPath, loggerFactory);
    logger.LogInformation("FixMarketDataConnector created");
    
    var isLoggedIn = false;
    
    // Subscribe to events
    connector.OnMarketDataReceived += (sender, snapshot) =>
    {
        logger.LogInformation("{Symbol}: Bid {BidPrice:F2}@{BidSize:F8} Ask {AskPrice:F2}@{AskSize:F8} Last {LastPrice:F2}@{LastSize:F8}",
            snapshot.Symbol, snapshot.BidPrice, snapshot.BidSize, snapshot.AskPrice, snapshot.AskSize,
            snapshot.LastPrice, snapshot.LastSize);
    };

    connector.OnStatusChanged += (sender, status) =>
    {
        logger.LogInformation("Status: {Status}", status);
        if (status == "Logged In")
        {
            isLoggedIn = true;
        }
    };

    // Load FIX settings
    var settings = new SessionSettings("config/fix_config.cfg");
    var storeFactory = new FileStoreFactory(settings);
    // Use QuickFIX logger that respects our log level
    var quickfixLogFactory = new QuickFixLogFactory(loggerFactory);

    // Create initiator
    var initiator = new SocketInitiator(connector.Application, storeFactory, settings, quickfixLogFactory);

    // Start the connection
    initiator.Start();
    logger.LogInformation("FIX Initiator started.");

    // Wait for logon with timeout
    var maxWaitSeconds = 15;
    var waitedSeconds = 0;
    while (!isLoggedIn && waitedSeconds < maxWaitSeconds)
    {
        Thread.Sleep(1000);
        waitedSeconds++;
        if (waitedSeconds % 5 == 0)
        {
            logger.LogDebug("Waiting for logon... ({WaitedSeconds}s)", waitedSeconds);
        }
    }

    if (!isLoggedIn)
    {
        logger.LogError("Failed to establish FIX session. Check logs for details.");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        initiator.Stop();
        return;
    }

    logger.LogInformation("FIX session established!");
    
    // Subscribe to market data
    var symbols = new[] { ("BTCUSDC", "MD001"), ("ETHUSDC", "MD002") };
    foreach (var (symbol, mdStreamId) in symbols)
    {
        logger.LogInformation("Subscribing to {Symbol}...", symbol);
        connector.SubscribeMarketData(symbol, mdStreamId);
    }

    // Keep running until user quits
    while (true)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Q) break;

        if (key.Key == ConsoleKey.S)
        {
            logger.LogInformation("=== BEGIN Latest Market Data Snapshot ===");
            foreach (var (symbol, _) in symbols)
            {
                var snapshot = connector.GetLatestSnapshot(symbol);
                if (snapshot != null)
                    logger.LogInformation("{Symbol}: Bid {BidPrice:F2}@{BidSize:F8} Ask {AskPrice:F2}@{AskSize:F8} Last {LastPrice:F2}@{LastSize:F8}",
                        symbol, snapshot.BidPrice, snapshot.BidSize, snapshot.AskPrice, snapshot.AskSize,
                        snapshot.LastPrice, snapshot.LastSize);
            }
            logger.LogInformation("=== END Latest Market Data Snapshot ===");
        }
    }

    // Cleanup
    logger.LogInformation("Shutting down...");
    foreach (var (_, id) in symbols)
        connector.UnsubscribeMarketData(id);
    Thread.Sleep(1000);

    connector.Logout();
    Thread.Sleep(2000);
    initiator.Stop();
    logger.LogInformation("Disconnected.");
}
catch (Exception ex)
{
    logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
}