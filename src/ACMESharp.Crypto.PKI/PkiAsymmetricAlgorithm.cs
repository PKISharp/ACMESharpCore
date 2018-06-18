namespace ACMESharp.Crypto.PKI
{
    public enum PkiAsymmetricAlgorithm
    {
        Unknown = 0,

        /// <summary>
        /// RSA (Rivest–Shamir–Adleman) is one of the first public-key cryptosystems
        /// and is widely used for secure data transmission.
        /// </summary>
        Rsa = 1,
        
        /// <summary>
        /// The Elliptic Curve Digital Signature Algorithm (ECDSA) offers a variant
        /// of the Digital Signature Algorithm (DSA) which uses elliptic curve
        /// cryptography.
        /// </summary>
        Ecdsa = 2,
    }
}