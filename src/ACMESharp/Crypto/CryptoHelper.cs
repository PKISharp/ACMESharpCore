namespace ACMESharp.Crypto
{
    /// <summary>
    /// For the most compatibility with LE, see:
    ///   https://letsencrypt.org/docs/integration-guide/#supported-key-algorithms
    /// We should support:
    /// * RSA Keys (2048-4096 bits)
    /// * ECDSA Keys (P-256, P-384)
    /// 
    /// Thats' for both account keys and cert keys.
    /// Partial classes support Base64, Ec, and Rsa tools
    /// </summary>
    public static partial class CryptoHelper
    {
        //Partial classes support Base64, Ec, and Rsa tools
    }
}