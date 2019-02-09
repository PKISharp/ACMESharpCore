using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using static PKISharp.SimplePKI.PkiKeyPair;

namespace PKISharp.SimplePKI
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
        public byte[] Export(PkiEncodingFormat format, char[] password = null)
        {
            switch (format)
            {
                case PkiEncodingFormat.Pem:
                    using (var sw = new StringWriter())
                    {
                        object pemObject = NativeKey;
                        if (IsPrivate && password != null)
                        {
                            var pkcs8Gen = new Pkcs8Generator(NativeKey,
                                    Pkcs8Generator.PbeSha1_3DES);
                            pkcs8Gen.Password = password;
                            pemObject = pkcs8Gen.Generate();
                        }
                        var pemWriter = new PemWriter(sw);
                        pemWriter.WriteObject(pemObject);
                        return Encoding.UTF8.GetBytes(sw.GetStringBuilder().ToString());
                    }

                case PkiEncodingFormat.Der:
                    if (IsPrivate)
                    {
                        var keyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(NativeKey);
                        return keyInfo.GetDerEncoded();
                    }
                    else
                    {
                        var keyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(NativeKey);
                        return keyInfo.GetDerEncoded();
                    }
                
                default:
                    throw new NotSupportedException();
            }
        }


        [XmlType(nameof(PkiKey))]
        [XmlInclude(typeof(PkiKeyPairRsaParams))]
        [XmlInclude(typeof(PkiKeyPairEcdsaParams))]
        public class RecoverableSerialForm
        {
            public RecoverableSerialForm()
            { }

            public RecoverableSerialForm(PkiKey key)
            {
                _algorithm = key.Algorithm;
                _key = key.Export(PkiEncodingFormat.Der);
                _isPrivate = key.IsPrivate;
            }

            public int _ver = 2;
            public PkiAsymmetricAlgorithm _algorithm;
            public bool _isPrivate;
            public byte[] _key;
            public PkiKey Recover()
            {
                if (_isPrivate)
                {
                    return new PkiKey(PrivateKeyFactory.CreateKey(_key), _algorithm);
                }
                else
                {
                    return new PkiKey(PublicKeyFactory.CreateKey(_key), _algorithm);
                }
            }
        }
    }
}