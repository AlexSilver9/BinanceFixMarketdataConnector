using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using QuickFix;
using QuickFix.Fields;

namespace BinanceFixMarketdataConnector;

/// <summary>
/// Handles Ed25519 signature generation and key management for Binance FIX API authentication
/// </summary>
public class Ed25519AuthenticationHandler
{
    private readonly string _apiKey;
    private readonly Ed25519PrivateKeyParameters _privateKeyParams;

    /// <summary>
    /// Creates a new authentication handler by loading the private key from a PEM file
    /// </summary>
    /// <param name="apiKey">Binance API key</param>
    /// <param name="privateKeyPath">Path to PEM-encoded Ed25519 private key</param>
    public Ed25519AuthenticationHandler(string apiKey, string privateKeyPath)
    {
        _apiKey = apiKey;
        _privateKeyParams = LoadPrivateKeyFromPem(privateKeyPath);
    }

    /// <summary>
    /// Creates a new authentication handler with an already-loaded private key
    /// </summary>
    /// <param name="apiKey">Binance API key</param>
    /// <param name="privateKeyParams">Pre-loaded Ed25519 private key parameters</param>
    public Ed25519AuthenticationHandler(string apiKey, Ed25519PrivateKeyParameters privateKeyParams)
    {
        _apiKey = apiKey;
        _privateKeyParams = privateKeyParams;
    }

    /// <summary>
    /// Loads an Ed25519 private key from a PEM file
    /// </summary>
    /// <param name="pemFilePath">Path to the PEM file</param>
    /// <returns>Ed25519 private key parameters</returns>
    /// <exception cref="InvalidOperationException">PEM file contains unexpected key type</exception>
    private static Ed25519PrivateKeyParameters LoadPrivateKeyFromPem(string pemFilePath)
    {
        using var reader = File.OpenText(pemFilePath);
        var pemReader = new PemReader(reader);
        var keyObject = pemReader.ReadObject();

        if (keyObject is AsymmetricCipherKeyPair keyPair)
        {
            return (Ed25519PrivateKeyParameters)keyPair.Private;
        }

        if (keyObject is Ed25519PrivateKeyParameters privateKey)
        {
            return privateKey;
        }

        throw new InvalidOperationException($"Unexpected key type: {keyObject?.GetType().Name}");
    }
    /// <summary>Signs logon message with Ed25519 signature, adds Binance-required authentication fields</summary>
    public void PrepareAndSignLogonMessage(Message message)
    {
        // Add RecvWindow to header
        message.Header.SetField(new IntField(FixConstants.BinanceFields.RecvWindow, 5000));

        // Build signature payload: A|SenderCompID|TargetCompID|MsgSeqNum|SendingTime
        var signPayload = $"{FixConstants.MsgTypes.Logon}{FixConstants.SOH}" +
                         $"{message.Header.GetString(Tags.SenderCompID)}{FixConstants.SOH}" +
                         $"{message.Header.GetString(Tags.TargetCompID)}{FixConstants.SOH}" +
                         $"{message.Header.GetString(Tags.MsgSeqNum)}{FixConstants.SOH}" +
                         $"{message.Header.GetString(Tags.SendingTime)}";

        // Generate Ed25519 signature
        var signer = new Ed25519Signer();
        signer.Init(true, _privateKeyParams);
        var dataToSign = Encoding.ASCII.GetBytes(signPayload);
        signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
        var signature = Convert.ToBase64String(signer.GenerateSignature());

        // Add Binance required logon fields
        message.SetField(new EncryptMethod(EncryptMethod.NONE));
        if (!message.IsSetField(Tags.HeartBtInt))
            message.SetField(new HeartBtInt(30));
        message.SetField(new RawDataLength(signature.Length));
        message.SetField(new RawData(signature));
        message.SetField(new ResetSeqNumFlag(true));
        message.SetField(new Username(_apiKey));
        message.SetField(new IntField(FixConstants.BinanceFields.MessageHandling, FixConstants.MessageHandlingValues.Unordered));
    }
}
