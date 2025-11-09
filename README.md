# Binance FIX Market Data Connector

A .NET 9.0 console application that connects to Binance's FIX protocol market data feed for real-time cryptocurrency market data streaming.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![FIX Protocol](https://img.shields.io/badge/FIX-4.4-green.svg)](https://www.fixtrading.org/)

## Features

- ✅ **Real-time market data** via Binance FIX API (WebSocket alternative)
- ✅ **Ed25519 signature authentication** for secure API access
- ✅ **FIX 4.4 protocol** implementation using QuickFIXn
- ✅ **Order book snapshots** and incremental updates
- ✅ **BTC/USDC & ETH/USDC symbol subscriptions** with efficient caching
- ✅ **Configurable logging** with structured output
- ✅ **Environment variable support** for credential management
- ✅ **SSL/TLS support** via stunnel proxy

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Architecture](#architecture)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Prerequisites

### Required

- **.NET 9.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Binance Account** with FIX API access enabled ([Binance](https://www.binance.com))
- **Ed25519 Key Pair** registered with your Binance API key - Setup instructions below
- **stunnel** (for SSL/TLS proxy) - Installation instructions below

### Platform Support

- Windows 10/11
- macOS 10.15+
- Linux (Ubuntu 20.04+, Debian 10+)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/AlexSilver9/BinanceFixMarketdataConnector.git
cd BinanceFixMarketdataConnector
```

### 2. Install stunnel (SSL/TLS Proxy)

#### macOS
```bash
brew install stunnel
```

#### Ubuntu/Debian
```bash
sudo apt-get install stunnel4
```

#### Windows
Download from [stunnel.org](https://www.stunnel.org/downloads.html)

### 3. Configure stunnel

Copy the example stunnel configuration:

```bash
cp BinanceFixMarketdataConnector/stunnel.conf.example BinanceFixMarketdataConnector/stunnel.conf
```

The default configuration connects to `fix-md.binance.com:9000` and exposes `localhost:9001`.

Start stunnel:

```bash
# macOS/Linux
stunnel BinanceFixMarketdataConnector/stunnel.conf

# Windows
stunnel.exe BinanceFixMarketdataConnector\stunnel.conf
```

### 4. Set Up Credentials

#### Generate Ed25519 Key Pair

Follow [Binance's guide](https://www.binance.com/en/support/faq/detail/6b9a63f1e3384cf48a2eedb82767a69a) to generate your Ed25519 key pair and register it with your Binance API key.
The Binance API key **must** have at least `FIX READING` permission set. 

#### Configure API Credentials

**Option A: Using Files (Recommended for Development)**

```bash
cd BinanceFixMarketdataConnector

# Create your actual credentials (see example files)
nano keys/api.key  # Paste your Binance API key
nano keys/private.ed25519.pem  # Paste your private key (PEM format)
```

**Option B: Using Environment Variables (Recommended for Production)**

```bash
export BINANCE_API_KEY="your-binance-api-key"
export BINANCE_PRIVATE_KEY_PATH="/absolute/path/to/private.ed25519.pem"
export LOG_LEVEL="Information"  # Optional: Debug, Information, Warning, Error
```

**Credential Priority:**
1. Environment variables (highest)
2. Files in `keys/` directory
3. `config/settings.json` (lowest)

### 5. Build the Project

```bash
dotnet build
```

## Configuration

### Application Settings

Copy the example and edit `BinanceFixMarketdataConnector/config/settings.json` for your needs:

```json
{
  "Binance": {
    "ApiKeyPath": "keys/api.key",
    "ApiKey": "",
    "PrivateKeyPath": "keys/private.ed25519.pem"
  },
  "Logging": {
    "LogLevel": "Information"
  }
}
```

### FIX Session Configuration

Choose a `SenderCompID` that matches the [Binance specifications](https://developers.binance.com/docs/binance-spot-api-docs/fix-api#message-components) (some max 8 character string should do it) .

Edit `BinanceFixMarketdataConnector/config/fix_config.cfg`:

```ini
[DEFAULT]
ConnectionType=initiator
SocketConnectHost=127.0.0.1
SocketConnectPort=9001
SenderCompID=<PUT_YOUR_SENDER_COMP_ID_HERE>
TargetCompID=SPOT
HeartBtInt=30
```

**Important:** `SocketConnectHost` and `SocketConnectPort` should point to your stunnel proxy, not directly to Binance.

### Subscribing to Symbols

Edit symbols in `BinanceFixMarketdataConnector/BinanceFixMarketdataConnector/Program.cs`:

```csharp
var symbols = new[] {
    ("BTCUSDC", "MD001"),
    ("ETHUSDC", "MD002"),
    ("SOLUSDC", "MD003")  // Add more symbols
};
```

## Usage

### Run the Application

```bash
# From solution directory
cd BinanceFixMarketdataConnector
dotnet clean
dotnet restore
dotnet run
```

### Interactive Commands

Once running, use these keyboard commands:

- **q/Q** - Quit application and disconnect
- **s/S** - Show latest market data snapshot for all subscribed symbols

### Example Output

```
2025-01-09T12:34:56.789Z info: FIX 4.4 Market Data Connector
2025-01-09T12:34:56.790Z info: Press 'Q' to quit
2025-01-09T12:34:56.790Z info: Press 'S' to show latest snapshots
2025-01-09T12:34:57.123Z info: FIX Initiator started.
2025-01-09T12:34:58.456Z info: Status: Logged In
2025-01-09T12:34:58.457Z info: FIX session established!
2025-01-09T12:34:58.458Z info: Subscribing to BTCUSDC...
2025-01-09T12:34:58.789Z info: BTCUSDC: Bid 42150.50@1.25000000 Ask 42151.00@0.87000000 Last 42150.75@0.15000000
2025-01-09T12:34:59.123Z info: ETHUSDC: Bid 2234.25@5.50000000 Ask 2234.50@3.20000000 Last 2234.30@1.00000000
```

### Logging Levels

Configure log verbosity via environment variable or `config/settings.json`:

- **Trace** - Everything including QuickFIX internals
- **Debug** - Verbose with FIX messages and signatures
- **Information** (default) - Market data updates and events
- **Warning** - Important issues
- **Error** - Errors only

```bash
export LOG_LEVEL="Debug"
```

## Architecture

### High-Level Overview

```
┌─────────────────┐
│   Program.cs    │  Main entry point
└────────┬────────┘
         │
         ▼
┌─────────────────────────────┐
│ FixMarketDataConnector      │  Orchestrates FIX session
└────────┬────────────────────┘
         │
    ┌────┴────┬─────────────────┬──────────────────┐
    ▼         ▼                 ▼                  ▼
┌─────────┐ ┌──────────┐ ┌─────────────┐ ┌───────────────────┐
│QuickFIX │ │FIXApp    │ │MarketData   │ │Ed25519Auth        │
│Initiator│ │Callbacks │ │Handler      │ │Handler            │
└─────────┘ └──────────┘ └─────────────┘ └───────────────────┘
     │
     ▼
┌─────────────┐
│   stunnel   │  SSL/TLS proxy
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│ Binance FIX API     │
│ fix-md.binance.com  │
└─────────────────────┘
```

### Key Components

#### 1. **Ed25519AuthenticationHandler.cs**
- Loads Ed25519 private key from PEM file
- Generates signatures for FIX Logon messages
- Implements Binance-specific authentication fields

#### 2. **FixMarketDataConnector.cs**
- High-level API for subscribing/unsubscribing to market data
- Manages FIX session lifecycle
- Exposes events for market data updates

#### 3. **FixApplicationCallbacks.cs**
- Implements QuickFIX `IApplication` interface
- Handles ToAdmin/FromAdmin callbacks
- Processes session-level messages (Logon, Logout, Reject)

#### 4. **MarketDataHandler.cs**
- Processes market data messages (Snapshot, Incremental, Reject)
- Maintains in-memory cache of latest market data
- Parses FIX repeating groups for bid/ask/trade data

#### 5. **ApplicationConfiguration.cs**
- Loads configuration from `config/settings.json`
- Supports environment variable overrides
- Validates required credentials

### Authentication Flow

```
1. Application starts
   │
2. Load Ed25519 private key from PEM file
   │
3. QuickFIX initiates connection to stunnel (localhost:9001)
   │
4. stunnel proxies SSL/TLS to fix-md.binance.com:9000
   │
5. QuickFIX sends Logon (MsgType=A)
   │
6. Ed25519AuthenticationHandler constructs payload:
   "A|SenderCompID|TargetCompID|MsgSeqNum|SendingTime"
   │
7. Sign payload with Ed25519 private key → base64 signature
   │
8. Add signature to Logon message (RawData field)
   │
9. Binance validates signature with registered public key
   │
10. Logon successful → Session established
```

### FIX Message Flow

**Subscribe to Market Data:**
```
Client → Binance: MarketDataRequest (MsgType=V)
  - MDReqID: Unique request ID
  - SubscriptionRequestType: 1 (Snapshot + Updates)
  - MarketDepth: 1 (Best bid/ask)
  - NoRelatedSym: 1
  - Symbol: BTCUSDC
  - NoMDEntryTypes: 3 (Bid, Offer, Trade)
```

**Receive Market Data:**
```
Binance → Client: MarketDataSnapshot (MsgType=W)
  - Symbol: BTCUSDC
  - NoMDEntries: 3
    - [0] MDEntryType=0 (Bid), MDEntryPx=42150.50, MDEntrySize=1.25
    - [1] MDEntryType=1 (Offer), MDEntryPx=42151.00, MDEntrySize=0.87
    - [2] MDEntryType=2 (Trade), MDEntryPx=42150.75, MDEntrySize=0.15

Binance → Client: MarketDataIncremental (MsgType=X)
  - NoMDEntries: 1
    - MDUpdateAction=0 (New), MDEntryType=0 (Bid), MDEntryPx=42150.75, ...
```

## Troubleshooting

### Common Issues

#### 1. Logon Timeout

**Symptoms:** Application waits 15 seconds and reports "Failed to establish FIX session"

**Potenital Causes:**
- Invalid API key
- Private key doesn't match registered public key
- FIX API READ not enabled on Binance account
- stunnel not running or misconfigured

**Solutions:**
```bash
# Check stunnel is running
ps aux | grep stunnel

# Test connectivity to stunnel
telnet localhost 9001

# Verify API key permissions in Binance account settings
# Ensure "FIX READING" is enabled

# Check logs
tail -f BinanceFixMarketdataConnector/bin/Debug/net9.0/log/*.messages.current.log
```

#### 2. Signature Validation Failed

**Symptoms:** Receive FIX Reject (MsgType=3) or Logout (MsgType=5) with signature error

**Causes:**
- Private key file is corrupted or wrong format
- Public key registered with Binance doesn't match private key
- System clock is out of sync (affects SendingTime field)

**Solutions:**
```bash
# Verify PEM file format
openssl pkey -in keys/private.ed25519.pem -text -noout

# Check system time
date -u  # Should be close to actual UTC time

# Regenerate key pair and re-register with Binance
```

#### 3. Connection Refused

**Symptoms:** Cannot connect to localhost:9001

**Solutions:**
```bash
# Verify stunnel configuration
cat BinanceFixMarketdataConnector/stunnel.conf

# Start stunnel with verbose logging
stunnel -D 7 BinanceFixMarketdataConnector/stunnel.conf

# Check if port 9001 is in use by stunnel
lsof -i :9001  # macOS/Linux
netstat -ano | findstr :9001  # Windows
```

#### 4. Market Data Not Received

**Symptoms:** Logon successful but no market data updates

**Causes:**
- Symbol not available on Binance Spot
- Market data permissions not granted to API key
- Subscription request failed

**Solutions:**
```bash
# Check for MarketDataReject (MsgType=Y) in logs
grep "MDReqRejReason" bin/Debug/net9.0/log/*.messages.current.log

# Verify symbol exists (use USDC not USDT for FIX API)
# Correct: BTCUSDC, ETHUSDC
# Wrong: BTCUSDT, ETHUSDT (may not be available via FIX)

# Enable Debug logging to see subscription details
export LOG_LEVEL="Debug"
```

### Debug Logging

Enable debug logging to see detailed FIX messages:

```bash
export LOG_LEVEL="Debug"
cd BinanceFixMarketdataConnector
dotnet run
```

This will show:
- Outgoing FIX messages (ToAdmin, ToApp)
- Incoming FIX messages (FromAdmin, FromApp)
- Signature generation details
- Market data processing steps

### FIX Session Logs

QuickFIX logs are stored in:
```
BinanceFixMarketdataConnector/bin/Debug/net9.0/log/
├── FIX.4.4-{SenderCompID}-SPOT.event.current.log
└── FIX.4.4-{SenderCompID}-SPOT.messages.current.log
```

Where `{SenderCompID}` is configured in `config/fix_config.cfg` (default: `BFMConn`)

- **event.current.log** - Connection events, logon/logout
- **messages.current.log** - Raw FIX messages (human-readable)

## Project Structure

```
BinanceFixMarketdataConnector/
├── BinanceFixMarketdataConnector.sln          # Solution file
├── README.md                                   # This file
├── LICENSE                                     # MIT License
├── .gitignore                                  # Git ignore rules
│
└── BinanceFixMarketdataConnector/             # Main project
    ├── BinanceFixMarketdataConnector.csproj   # Project file
    ├── Program.cs                              # Entry point
    │
    ├── config/                                 # Configuration files
    │   ├── settings.json                       # App settings (gitignored)
    │   ├── settings.json.example              # Example settings
    │   ├── fix_config.cfg                      # QuickFIX configuration
    │   └── binance-spot-fix-md.xml            # FIX data dictionary
    │
    ├── keys/                                   # API credentials (gitignored)
    │   ├── api.key                             # Binance API key
    │   ├── api.key.example                    # Example API key
    │   ├── private.ed25519.pem                # Ed25519 private key
    │   └── private.ed25519.pem.example        # Example private key
    │
    ├── ApplicationConfiguration.cs             # Config loader
    ├── Ed25519AuthenticationHandler.cs        # Authentication
    ├── DebugLogger.cs                         # QuickFIX logger
    ├── FixApplicationCallbacks.cs             # QuickFIX callbacks
    ├── FixConstants.cs                        # FIX constants
    ├── FixMarketDataConnector.cs              # Main connector
    ├── MarketDataHandler.cs                   # Market data processor
    └── LoggingConfiguration.cs                # Logging setup
    
    
```

## Dependencies

### NuGet Packages

- **[QuickFIXn.Core](https://www.nuget.org/packages/QuickFIXn.Core/)** (1.14.0) - FIX protocol engine
- **[QuickFIXn.FIX44](https://www.nuget.org/packages/QuickFIXn.FIX44/)** (1.14.0) - FIX 4.4 message definitions
- **[BouncyCastle.Cryptography](https://www.nuget.org/packages/BouncyCastle.Cryptography/)** (2.6.2) - Ed25519 signatures and PEM parsing
- **[Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/)** (9.0.0) - Configuration framework
- **[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging/)** (9.0.0) - Logging framework

### External Dependencies

- **stunnel** - SSL/TLS proxy for secure connections

## Security Best Practices

### ⚠️ Never Commit Credentials

The following files are gitignored and must NEVER be committed:
- `keys/*.key` (except `*.key.example`)
- `keys/*.pem` (except `*.pem.example`)
- `config/settings.json`

### 🔐 Protect Your Private Key

```bash
# Set restrictive permissions on private key (Unix-like systems)
chmod 600 keys/private.ed25519.pem

# Never share your private key
# If compromised, immediately:
# 1. Revoke the API key in Binance
# 2. Generate new key pair
# 3. Re-register public key
```

### 🌍 Use Environment Variables in Production

```bash
# Production deployment
export BINANCE_API_KEY="..."
export BINANCE_PRIVATE_KEY_PATH="/secure/path/to/private.pem"

# Avoid storing credentials in files on production servers
```

## FAQ

### Q: Why use FIX instead of WebSocket API?

**A:** FIX protocol offers:
- Industry-standard for financial market data
- Better suited for institutional/enterprise use
- Lower latency for order book updates
- More reliable session management
- Persistent session state

### Q: Can I use this for trading (placing orders)?

**A:** This project focuses on **market data only** for now. Binance FIX API supports order entry,
    but this connector supports read-only market data subscriptions.
    For trading, you'd need to implement additional message types (NewOrderSingle, etc.).

### Q: What symbols are supported?

**A:** Check [Binance FIX documentation](https://developers.binance.com/docs/binance-spot-api-docs)
    for obtaining current symbol list.

### Q: How do I generate Ed25519 keys?

**A:** Check [Binance guide](https://www.binance.com/en/support/faq/detail/6b9a63f1e3384cf48a2eedb82767a69a)
for key generation.

Alterantively try use `ssh-keygen`:

```bash
ssh-keygen -t ed25519 -f private.ed25519
# Convert to PEM format if needed
ssh-keygen -p -m PEM -f private.ed25519
```

Then register the public key with your Binance API key in the account settings.

### Q: Can I run multiple instances?

**A:** As per current [Binance FIX documentation](https://developers.binance.com/docs/binance-spot-api-docs),
    each instance requires a unique `SenderCompID` in `fix_config.cfg`.
    Binance may have limits on concurrent sessions per API key.

### Q: Is this production-ready?

**A:** This is a reference implementation suitable for:
- ✅ Development and testing
- ✅ Market data monitoring
- ✅ Research and analysis

For production use, consider adding:
- Dynamic subscribtion management for market data streams (Currently hardcoded)
- Comprehensive error handling
- Metrics and monitoring
- Health checks
- Data persistence

## Resources

### Official Documentation

- [FIX Protocol 4.4 Specification](https://www.fixtrading.org/standards/fix-4-4/)
- [Binance FIX API Guide](https://developers.binance.com/docs/binance-spot-api-docs)
- [QuickFIXn Documentation](https://github.com/connamara/quickfixn)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) files for details.

This product includes software developed by quickfixengine.org (http://www.quickfixengine.org/).

## Disclaimer

This software is for educational and development purposes. Use at your own risk. The authors are not responsible for any financial losses incurred through the use of this software.

---

**Questions or Issues?** Please open an issue on GitHub or check existing discussions.

**Star this repo** ⭐ if you find it useful!
