namespace PKISharp.SimplePKI
{
    public enum PkiArchiveFormat
    {
        Unknown = 0,

        /// <summary>
        /// The PKCS#12 (.PFX) format.
        /// </summary>
        Pkcs12 = 3,

        /// <summary>
        /// PEM-encoded archive format.
        /// </summary>
        Pem = 4,
    }
}