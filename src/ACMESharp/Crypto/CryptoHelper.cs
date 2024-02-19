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
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// Returns a singleton instance of cryptographic tool
        /// for URL-safe Base64 encoding.
        /// </summary>
        public static Base64Tool Base64 { get; } = new Base64Tool();

        /// <summary>
        /// Returns a singleton instance of cryptographic tool
        /// for working with RSA keys and algorithms.
        /// </summary>
        public static RsaTool Rsa { get; } = new RsaTool();

        /// <summary>
        /// Returns a singleton instance of cryptographic tool
        /// for working with EC keys and algorithms.
        /// </summary>
        public static EcTool Ec { get; } = new EcTool();
    }
}