using Microsoft.Extensions.Logging;

namespace BinanceFixMarketdataConnector;

/// <summary>Console logging configuration</summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Creates a logger factory with console logging configured for single-line output with UTC timestamps
    /// </summary>
    /// <param name="logLevel">Log level string (trace/debug/info/warning/error/critical/none), defaults to Information if invalid</param>
    /// <returns>Configured ILoggerFactory</returns>
    public static ILoggerFactory CreateLoggerFactory(string? logLevel = null)
    {
        var level = ParseLogLevel(logLevel ?? Environment.GetEnvironmentVariable("LOG_LEVEL"));

        return LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(level)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                    options.UseUtcTimestamp = true;
                });
        });
    }

    private static LogLevel ParseLogLevel(string? level)
    {
        return level?.ToLower() switch
        {
            "trace" => LogLevel.Trace,
            "debug" => LogLevel.Debug,
            "information" or "info" => LogLevel.Information,
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "critical" => LogLevel.Critical,
            "none" => LogLevel.None,
            _ => LogLevel.Information
        };
    }
}
