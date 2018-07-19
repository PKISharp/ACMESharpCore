using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using PKISharp.SimplePKI.Util;

namespace PKISharp.SimplePKI
{
    /// <summary>
    /// A general abstraction of a public/private key pair for an asymmetric encryption algorithm.
    /// </summary>
    public class PkiKeyPair
    {
        private PkiKey _PublicKey;
        private PkiKey _PrivateKey;
        private Func<PkiKey, byte[], byte[]> _signer;
        private Func<PkiKey, byte[], byte[], bool> _verifier;
        private Func<PkiKeyPair, bool, object> _jwkExporter;

        internal PkiKeyPair(AsymmetricCipherKeyPair nativeKeyPair, PkiKeyPairParams kpParams)
        {
            NativeKeyPair = nativeKeyPair;
            Parameters = kpParams;
            Init();
        }

        public PkiKeyPairParams Parameters { get; }

        public PkiAsymmetricAlgorithm Algorithm => Parameters.Algorithm;

        public PkiKey PublicKey
        {
            get
            {
                if (_PublicKey == null)
                    _PublicKey = new PkiKey(NativeKeyPair.Public, Algorithm);
                return _PublicKey;
            }
        }

        public PkiKey PrivateKey
        {
            get
            {
                if (_PrivateKey == null)
                    _PrivateKey = new PkiKey(NativeKeyPair.Private, Algorithm);
                return _PrivateKey;
            }
        }

        internal AsymmetricCipherKeyPair NativeKeyPair { get; set; }

        private void Init()
        {
            switch (Algorithm)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    // SHA + ECDSA algor selection based on:
                    //    https://github.com/bcgit/bc-csharp/blob/master/crypto/src/security/SignerUtilities.cs
                    var sigAlgor = $"SHA{Parameters.HashBits}WITHRSA";
                    _signer = (pkey, data) => Sign(sigAlgor, pkey, data);
                    _verifier = (pkey, data, sig) => Verify(sigAlgor, pkey, data, sig);
                    _jwkExporter = (keys, prv) => ExportRsJwk(keys, prv);
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    // SHA + ECDSA algor selection based on:
                    //    https://github.com/bcgit/bc-csharp/blob/master/crypto/src/security/SignerUtilities.cs
                    // Transcode Length:
                    //    * lengths are specified as in:
                    //       https://tools.ietf.org/html/draft-ietf-jose-json-web-algorithms-24#section-3.4
                    //    * see explanation in the docs for "TranscodeSignatureToConcat" for what this is all about
                    var hashBits = Parameters.HashBits;
                    var transcodeLength = 0;
                    if (hashBits == -1)
                    {
                        switch (Parameters.Bits)
                        {
                            case 521: hashBits = 512; transcodeLength = 132; break;
                            case 384: hashBits = 384; transcodeLength = 96; break;
                            default : hashBits = 256; transcodeLength = 64; break;
                        }
                    }
                    sigAlgor = $"SHA{hashBits}WITHECDSA";
                    _signer = (prv, data) => Sign(sigAlgor, prv, data, transcodeLength);
                    _verifier = (pub, data, sig) => Verify(sigAlgor, pub, data, sig);
                    _jwkExporter = (keys, prv) => ExportEcJwk(Parameters.Bits, keys, prv);
                    break;
                default:
                    throw new NotSupportedException("Unsupported Algorithm");
            }
        }

        /// <summary>
        /// Generates an RSA key pair for the argument bit length.
        /// Some typical bit lengths include 1024, 2048 and 4096.
        /// It is generally agreed upon that modern use of RSA should
        /// require a minimum of 2048-bit key size.
        /// </summary>
        public static PkiKeyPair GenerateRsaKeyPair(int bits, int hashBits = 256)
        {
            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/pkcs/test/PKCS10Test.cs

            var rsaParams = new RsaKeyGenerationParameters(
                    BigInteger.ValueOf(0x10001), new SecureRandom(), bits, 25);
            var rsaKpGen = GeneratorUtilities.GetKeyPairGenerator("RSA");
            rsaKpGen.Init(rsaParams);
            var nativeKeyPair = rsaKpGen.GenerateKeyPair();

            return new PkiKeyPair(nativeKeyPair,
                    new PkiKeyPairRsaParams(bits) { HashBits = hashBits });
        }

        public static PkiKeyPair GenerateEcdsaKeyPair(int bits, int hashBits = -1)
        {
            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/ECTest.cs#L331
            //    https://www.codeproject.com/Tips/1150485/Csharp-Elliptical-Curve-Cryptography-with-Bouncy-C

            // This produced the following error against Let's Encrypt CA:
            //    ACMESharp.Protocol.AcmeProtocolException : Error parsing certificate request: asn1: structure error: tags don't match (6 vs {class:0 tag:16 length:247 isCompound:true}) {optional:false explicit:false application:false defaultValue:<nil> tag:<nil> stringType:0 timeType:0 set:false omitEmpty:false} ObjectIdentifier @3

            // var ecNistParams = NistNamedCurves.GetByName("P-" + bits);
            // var ecDomainParams = new ECDomainParameters(ecNistParams.Curve,
            //         ecNistParams.G, ecNistParams.N, ecNistParams.H, ecNistParams.GetSeed());
            // var ecParams = new ECKeyGenerationParameters(ecDomainParams, new SecureRandom());

            // So according to [this](https://github.com/golang/go/issues/18634#issuecomment-272527314)
            // it seems we were passing in arbitrary curve details instead of a named curve OID as we do here:

            var ecCurveOid = NistNamedCurves.GetOid("P-" + bits);;
            var ecParams = new ECKeyGenerationParameters(ecCurveOid, new SecureRandom());
            var ecKpGen = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            ecKpGen.Init(ecParams);
            var nativeKeyPair = ecKpGen.GenerateKeyPair();

            return new PkiKeyPair(nativeKeyPair,
                    new PkiKeyPairEcdsaParams(bits) { HashBits = hashBits });
        }

        /// <summary>
        /// Returns true if the underlying key pair algorithm supports signing.
        /// </summary>
        public bool CanSign => _signer != null;

        /// <summary>
        /// Signs the input data using the private key of this key pair if the
        /// underlying key pair algorithm supports signing.
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotSupportedException">When the underlying key pair implementation
        ///         does not support signing</exception>  
        public byte[] Sign(byte[] data)
        {
            if (_signer == null)
                throw new NotSupportedException();

            return _signer(this.PrivateKey, data);
        }

        public bool Verify(byte[] data, byte[] sig)
        {
            if (_verifier == null)
                throw new NotSupportedException();
            
            return _verifier(this.PublicKey, data, sig);
        }

        internal static byte[] Sign(string algor, PkiKey prv, byte[] input, int transcodeLength = 0)
        {
            // Based on:
            //    http://mytenpennies.wikidot.com/blog:using-bouncy-castle
            
            var signer = SignerUtilities.GetSigner(algor);
            signer.Init(true, prv.NativeKey);
            signer.BlockUpdate(input, 0, input.Length);
            var sig = signer.GenerateSignature();

            if (transcodeLength != 0)
            {
                sig = TranscodeSignatureToConcat(sig, transcodeLength);
            }

            return sig;
        }

        internal static bool Verify(string algor, PkiKey pub, byte[] input, byte[] sig)
        {
            // Based on:
            //    http://mytenpennies.wikidot.com/blog:using-bouncy-castle
            
            var signer = SignerUtilities.GetSigner(algor);
            signer.Init(false, pub.NativeKey);
            signer.BlockUpdate(input, 0, input.Length);
            return signer.VerifySignature(sig);
        }

        /// <summary>
        /// Transcodes the JCA ASN.1/DER-encoded signature into the concatenated
        /// R + S format expected by ECDSA JWS.
        /// </summary>
        /// <remarks>
        /// @param derSignature The ASN1./DER-encoded. Must not be {@code null}.
        /// @param outputLength The expected length of the ECDSA JWS signature.
        /// @return The ECDSA JWS encoded signature.
        /// <throws cref="JwtException">If the ASN.1/DER signature format is invalid.</throws>
        /// </remarks>
        public static byte[] TranscodeSignatureToConcat(byte[] derSignature, int outputLength)
        {
            // We discovered the long and hard way that not all ECDSA signatures are alike!
            // Turns out that BouncyCastle's implementation returns the ASN1/DER encoded
            // form which in some ways is the correct-est form, but also turns out this is
            // not what the .NET BCL libraries produce and it is not what the JWS form used
            // by ACME expects.
            //
            // Based on the following sources, we figured out the discrepency and also how
            // to convert it (using the code below which was originally a Java function
            // and left completely intact as it was copied over to C#!):
            //    * https://tools.ietf.org/html/draft-ietf-jose-json-web-algorithms-24#section-3.4
            //    * https://crypto.stackexchange.com/a/1797/59470
            //    * http://bouncy-castle.1462172.n4.nabble.com/Signature-wrong-length-using-ECDSA-using-P-521-td4658010.html
            //    * Java source code copied from down at the bottom of:
            //      * http://www.ssekhon.com/blog/2017/08/02/sign-data-using-ecdsa-and-bouncy-castle

            if (derSignature.Length < 8 || derSignature[0] != 48)
            {
                throw new Exception("Invalid ECDSA signature format");
            }

            int offset;
            if (derSignature[1] > 0)
            {
                offset = 2;
            }
            else if (derSignature[1] == (byte)0x81)
            {
                offset = 3;
            }
            else
            {
                throw new Exception("Invalid ECDSA signature format");
            }

            byte rLength = derSignature[offset + 1];

            int i = rLength;
            while ((i > 0)
                    && (derSignature[(offset + 2 + rLength) - i] == 0))
                i--;

            byte sLength = derSignature[offset + 2 + rLength + 1];

            int j = sLength;
            while ((j > 0)
                    && (derSignature[(offset + 2 + rLength + 2 + sLength) - j] == 0))
                j--;

            int rawLen = Math.Max(i, j);
            rawLen = Math.Max(rawLen, outputLength / 2);

            if ((derSignature[offset - 1] & 0xff) != derSignature.Length - offset
                    || (derSignature[offset - 1] & 0xff) != 2 + rLength + 2 + sLength
                    || derSignature[offset] != 2
                    || derSignature[offset + 2 + rLength] != 2)
            {
                throw new Exception("Invalid ECDSA signature format");
            }

            byte[] concatSignature = new byte[2 * rawLen];

            Array.Copy(derSignature, (offset + 2 + rLength) - i, concatSignature, rawLen - i, i);
            Array.Copy(derSignature, (offset + 2 + rLength + 2 + sLength) - j, concatSignature, 2 * rawLen - j, j);

            return concatSignature;
        }

        public object ExportJwk(bool @private = false)
        {
            return _jwkExporter == null ? null : _jwkExporter(this, @private);
        }

        // Helpful for debugging:
        // public object ExportEcParameters()
        // {
        //     var pub = (ECPublicKeyParameters)_PublicKey.NativeKey;
        //     var prv = (ECPrivateKeyParameters)_PrivateKey.NativeKey;

        //     var exp = new
        //     {
        //         HashSize = prv.D.ToByteArrayUnsigned().Length * 8,
        //         D = prv.D.ToByteArrayUnsigned(),
        //         X = pub.Q.XCoord.GetEncoded(),
        //         Y = pub.Q.YCoord.GetEncoded(),
        //     };
        //     return exp;

        // }

        internal static object ExportRsJwk(PkiKeyPair keys, bool @private)
        {
            if (@private)
                throw new NotImplementedException();

            var pub = (RsaKeyParameters)keys.PublicKey.NativeKey;
            return new
            {
                // As per RFC 7638 Section 3, these are the *required* elements of the
                // JWK and are sorted in lexicographic order to produce a canonical form

                e = Base64Tool.Instance.UrlEncode(pub.Exponent.ToByteArray()),
                kty = "RSA", // https://tools.ietf.org/html/rfc7518#section-6.3
                n = Base64Tool.Instance.UrlEncode(pub.Modulus.ToByteArray()),
            };
        }

        internal static object ExportEcJwk(int bits, PkiKeyPair keys, bool @private)
        {
            if (@private)
                throw new NotImplementedException();
            
            var pub = (ECPublicKeyParameters)keys.PublicKey.NativeKey;
            return new
            {
                // As per RFC 7638 Section 3, these are the *required* elements of the
                // JWK and are sorted in lexicographic order to produce a canonical form

                crv = $"P-{bits}",
                kty = "EC", // https://tools.ietf.org/html/rfc7518#section-6.2
                x = Base64Tool.Instance.UrlEncode(pub.Q.XCoord.GetEncoded()),
                y = Base64Tool.Instance.UrlEncode(pub.Q.YCoord.GetEncoded()),
            };
        }

        /// <summary>
        /// Saves this key pair instance to the target stream,
        /// in a recoverable serialization format.
        /// </summary>
        /// <param name="stream"></param>
        public void Save(Stream stream)
        {
            var xmlSer = new XmlSerializer(typeof(RecoverableSerialForm));
            var ser = new RecoverableSerialForm(this);
            xmlSer.Serialize(stream, ser);
        }

        /// <summary>
        /// Recovers a serialized key pair previously saved using
        /// a recoverable serialization format.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static PkiKeyPair Load(Stream stream)
        {
            var xmlSer = new System.Xml.Serialization.XmlSerializer(typeof(RecoverableSerialForm));
            var ser = (RecoverableSerialForm)xmlSer.Deserialize(stream);
            return ser.Recover();
        }

        [XmlType(nameof(PkiKeyPair))]
        [XmlInclude(typeof(PkiKeyPairRsaParams))]
        [XmlInclude(typeof(PkiKeyPairEcdsaParams))]
        public class RecoverableSerialForm
        {
            public RecoverableSerialForm()
            { }

            public RecoverableSerialForm(PkiKeyPair keyPair)
            {
                _algorithm = keyPair.Algorithm;
                _privateKey = keyPair.PrivateKey.Export(PkiEncodingFormat.Der);
                _publicKey = keyPair.PublicKey.Export(PkiEncodingFormat.Der);
                _kpParams = keyPair.Parameters;
            }

            public int _ver = 2;
            public PkiAsymmetricAlgorithm _algorithm;
            public byte[] _privateKey;
            public byte[] _publicKey;
            public PkiKeyPairParams _kpParams;
            public PkiKeyPair Recover()
            {
                var pubKey = PublicKeyFactory.CreateKey(_publicKey);
                var prvKey = PrivateKeyFactory.CreateKey(_privateKey);

                return new PkiKeyPair(null, _kpParams)
                {
                    _PrivateKey = new PkiKey(prvKey, _algorithm),
                    _PublicKey = new PkiKey(pubKey, _algorithm),
                };
            }
        }

        public class PkiKeyPairParams
        {
            internal PkiKeyPairParams()
            { }

            public PkiAsymmetricAlgorithm Algorithm { get; set; } = PkiAsymmetricAlgorithm.Unknown;

            public int Bits { get; set; }

            public int HashBits { get; set; }
        }

        public class PkiKeyPairRsaParams : PkiKeyPairParams
        {
            internal PkiKeyPairRsaParams()
            { }

            public PkiKeyPairRsaParams(int bits)
            {
                Algorithm = PkiAsymmetricAlgorithm.Rsa;
                Bits = bits;
                HashBits = 256;
            }
        }

        public class PkiKeyPairEcdsaParams : PkiKeyPairParams
        {
            internal PkiKeyPairEcdsaParams()
            { }

            public PkiKeyPairEcdsaParams(int bits)
            {
                Algorithm = PkiAsymmetricAlgorithm.Ecdsa;
                Bits = bits;
            }
        }
    }
}