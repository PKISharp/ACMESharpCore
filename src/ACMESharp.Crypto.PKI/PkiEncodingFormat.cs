namespace ACMESharp.Crypto.PKI
{
    public enum PkiEncodingFormat
    {
        Unknown = 0,

        /// <summary>
        /// DER binary encoding.
        /// </summary>
        Der = 1,

        /// <summary>
        /// PEM text encoding.
        /// </summary>
        Pem = 2,
    }
}