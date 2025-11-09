using Microsoft.Extensions.Configuration;

namespace BinanceFixMarketdataConnector;

/// <summary>
/// Application configuration loaded from settings.json
/// </summary>
public class ApplicationConfiguration
{
    public BinanceConfiguration Binance { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();

    /// <summary>
    /// Loads configuration from `settings.json` with optional environment variable overrides
    /// </summary>
    public static ApplicationConfiguration Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "BINANCE_")
            .Build();

        var config = new ApplicationConfiguration();
        configuration.Bind(config);

        // Load API key from file if ApiKeyPath is specified
        if (!string.IsNullOrWhiteSpace(config.Binance.ApiKeyPath) && File.Exists(config.Binance.ApiKeyPath))
        {
            config.Binance.ApiKey = File.ReadAllText(config.Binance.ApiKeyPath).Trim();
        }

        // Override with environment variables if present (highest priority)
        config.Binance.ApiKey = Environment.GetEnvironmentVariable("BINANCE_API_KEY")
            ?? config.Binance.ApiKey;
        config.Binance.PrivateKeyPath = Environment.GetEnvironmentVariable("BINANCE_PRIVATE_KEY_PATH")
            ?? config.Binance.PrivateKeyPath;
        config.Logging.LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL")
            ?? config.Logging.LogLevel;

        return config;
    }

    /// <summary>
    /// Validates that all required configuration values are present
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Binance.ApiKey))
            throw new InvalidOperationException("Binance API Key is required. Set it in settings.json or BINANCE_API_KEY environment variable.");

        if (string.IsNullOrWhiteSpace(Binance.PrivateKeyPath))
            throw new InvalidOperationException("Private key path is required. Set it in settings.json or BINANCE_PRIVATE_KEY_PATH environment variable.");

        if (!File.Exists(Binance.PrivateKeyPath))
            throw new FileNotFoundException($"Private key file not found: {Binance.PrivateKeyPath}");
    }
}

public class BinanceConfiguration
{
    public string ApiKeyPath { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
}

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
}
