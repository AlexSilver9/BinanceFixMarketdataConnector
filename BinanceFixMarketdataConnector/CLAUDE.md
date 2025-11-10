# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 9.0 console application that connects to Binance's FIX protocol market data feed. It implements a FIX 4.4 client using the QuickFIXn library to subscribe to real-time cryptocurrency market data (order book snapshots and incremental updates).

## Key Architecture Components

### FIX Protocol Implementation
- **FixMarketDataConnector.cs**: High-level connector for FIX session management
- **FixApplicationCallbacks.cs**: Implements QuickFIX `IApplication` callbacks
  - Handles session lifecycle (OnCreate, OnLogon, OnLogout)
  - Delegates to Ed25519AuthenticationHandler for authentication
  - Delegates to MarketDataHandler for market data processing
- **Ed25519AuthenticationHandler.cs**: Generates Ed25519 signatures for authentication
- **MarketDataHandler.cs**: Processes market data messages (W, X, Y), maintains in-memory cache

### Authentication Flow
The Logon message (MsgType='A') requires Ed25519 signature authentication:
1. Constructs payload from: `MsgType|SenderCompID|TargetCompID|MsgSeqNum|SendingTime` (SOH-delimited)
2. Signs payload with Ed25519 private key loaded from `keys/private.ed25519.pem`
3. Includes required Binance-specific fields:
   - Field 95/96: RawDataLength/RawData (signature)
   - Field 141: ResetSeqNumFlag (must be Y)
   - Field 553: Username (API key)
   - Field 25035: MessageHandling (1=UNORDERED, 2=SEQUENTIAL)

### Message Processing
- **ToAdmin/FromAdmin**: Handles administrative messages (Logon, Logout, Heartbeat, Reject)
- **ToApp/FromApp**: Handles application messages (Market Data requests and responses)
- **OnMessage**: Dispatches FIX 4.4 messages to appropriate handlers
- Market data updates trigger `OnMarketDataReceived` event with snapshot data

## Configuration Files

- **config/settings.json**: Application configuration (gitignored)
  - API key path or value
  - Private key path
  - Log level
  - Symbols: List of trading symbols to subscribe for market data
- **config/fix_config.cfg**: QuickFIX session configuration
  - Connection: localhost:9001 (stunnel proxy to fix-md.binance.com:9000)
  - Session IDs: SenderCompID=BFMConn, TargetCompID=SPOT
  - Uses custom data dictionary: config/binance-spot-fix-md.xml
  - File-based message store and logging
- **config/binance-spot-fix-md.xml**: FIX data dictionary with Binance-specific fields
- **keys/api.key**: Binance API key (gitignored)
- **keys/private.ed25519.pem**: Ed25519 private key for authentication (gitignored)

## Development Commands

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Clean Build Artifacts
```bash
dotnet clean
```

### Run from Binary
```bash
./bin/Debug/net9.0/BinanceFixMarketdataConnector
```

## Dependencies

- **QuickFIXn.Core** (1.14.0): FIX protocol engine
- **QuickFIXn.FIX44** (1.14.0): FIX 4.4 message definitions
- **BouncyCastle.Cryptography** (2.6.2): Ed25519 signature generation and PEM key parsing

## Runtime Behavior

The application follows this flow:
1. Loads configuration from `config/settings.json` with environment variable overrides
2. Ed25519AuthenticationHandler loads private key from `keys/private.ed25519.pem`
3. Creates FixMarketDataConnector with API credentials
4. Initializes QuickFIX SocketInitiator with file store and logging
5. Waits up to 15 seconds for successful logon
6. Subscribes to market data for symbols defined in configuration (default: BTCUSDC, ETHUSDC)
   - Market data request IDs are generated dynamically using pattern: `MD-{symbol}-{counter:D3}`
   - Example: BTCUSDC → MD-BTCUSDC-001, ETHUSDC → MD-ETHUSDC-002
7. Continuously displays market data updates until user quits (Q key)
8. Unsubscribes and performs graceful logout on exit

Session state is persisted in `bin/Debug/net9.0/store/`, logs in `bin/Debug/net9.0/log/`.

## Troubleshooting Authentication Issues

### Common Reasons for Logon Timeout

1. **Invalid API Key**
   - The API key in `keys/api.key` (or env var) must be valid and active
   - FIX API access must be enabled on the Binance account
   - The key must have at least "FIX READING" permission

2. **Incorrect Private Key**
   - The `keys/private.ed25519.pem` must match the public key registered with the API key
   - Verify key format is correct (PEM-encoded Ed25519)

3. **Signature Calculation Issues**
   - Signature payload format: `A|SenderCompID|TargetCompID|MsgSeqNum|SendingTime` (SOH-delimited)
   - Must use ASCII encoding before signing
   - Signature must be base64-encoded

4. **Connection Issues**
   - Verify `fix-md.binance.com:9000` is accessible
   - Check SSL/TLS connection is established
   - Review firewall/network settings

### Diagnostic Steps

1. **Check FIX logs**: Review files in `bin/Debug/net9.0/log/`
   - `*.event.current.log` - Connection events
   - `*.messages.current.log` - Raw FIX messages

2. **Look for Reject/Logout messages**: If Binance responds with message type 3 (Reject) or 5 (Logout), the error details will show the authentication failure reason

3. **Verify signature calculation**: Use the test values from Binance documentation:
   - MsgType: `A`
   - SenderCompID: `EXAMPLE`
   - TargetCompID: `SPOT`
   - MsgSeqNum: `1`
   - SendingTime: `20240627-11:17:25.223`
   - Expected signature: `4MHXelVVcpkdwuLbl6n73HQUXUf1dse2PCgT1DYqW9w8AVZ1RACFGM+5UdlGPrQHrgtS3CvsRURC1oj73j8gCA==`

### API Key Security

**IMPORTANT**: Never commit API keys to source control. The following files are gitignored:
- `keys/api.key`
- `keys/*.pem` (except `*.pem.example`)
- `config/settings.json`

Use environment variables for production:
- `BINANCE_API_KEY`
- `BINANCE_PRIVATE_KEY_PATH`
- `BINANCE_SYMBOLS` (comma or semicolon separated, e.g., `BTCUSDC,ETHUSDC,SOLUSDC`)
- `LOG_LEVEL`
