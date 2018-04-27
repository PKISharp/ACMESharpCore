using System;
using System.IO;

namespace ACMESharp.Crypto.JOSE
{
    /// <summary>
    /// Defines the interface for a tool that provides the required
    /// JOSE Web Signature (JWS) functions as used by the ACME protocol.
    /// </summary>
    public interface IJwsTool : IDisposable
    {
        string JwsAlg
        { get; }

        void Init();

        void Save(Stream stream);

        void Load(Stream stream);

        object ExportJwk(bool canonical = false);

        byte[] Sign(byte[] raw);
    }
}
