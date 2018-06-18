using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace ACMESharp.Crypto.PKI
{
    /// <summary>
    /// A general abstraction for an asymmetric algorithm key, either public or private.
    /// </summary>
    public class PkiKey
    {
        internal PkiKey(AsymmetricKeyParameter nativeKey,
            PkiAsymmetricAlgorithm algorithm = PkiAsymmetricAlgorithm.Unknown)
        {
            NativeKey = nativeKey;
            Algorithm = algorithm;
        }

        public PkiAsymmetricAlgorithm Algorithm { get; }

        /// <summary>
        /// True if this key is the private key component of a key pair.
        /// </summary>
        public bool IsPrivate => NativeKey.IsPrivate;

        internal AsymmetricKeyParameter NativeKey { get; }

        /// <summary>
        /// Exports the key into a supported key format.
        /// </summary>
        public byte[] Export(PkiEncodingFormat format)
        {
            switch (format)
            {
                case PkiEncodingFormat.Pem:
                    using (var sw = new StringWriter())
                    {
                        var pemWriter = new PemWriter(sw);
                        pemWriter.WriteObject(NativeKey);
                        return Encoding.UTF8.GetBytes(sw.GetStringBuilder().ToString());
                    }

                case PkiEncodingFormat.Der:
                    var pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(NativeKey);
                    return pkInfo.GetDerEncoded();
                
                default:
                    throw new NotSupportedException();
            }
        }
    }
}