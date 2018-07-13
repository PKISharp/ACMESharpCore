using System;
using System.IO;

namespace ACMESharp.Crypto.JOSE
{
    /// <summary>
    /// Defines the interface for a tool that provides the required
    /// JOSE Web Signature (JWS) functions as used by the ACME protocol.
    /// </summary>
    public interface IJwsSigner : IDisposable
    {
        string JwsAlg
        { get; }

        string ExportAlgorithm();

        void Import(string privateJwk);

        object ExportPublicJwk();

        byte[] Sign(byte[] raw);
    }
}
