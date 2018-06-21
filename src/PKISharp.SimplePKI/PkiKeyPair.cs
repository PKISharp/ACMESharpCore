using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace PKISharp.SimplePKI
{
    /// <summary>
    /// A general abstraction of a public/private key pair for an asymmetric encryption algorithm.
    /// </summary>
    public class PkiKeyPair
    {
        private PkiKey _PublicKey;
        private PkiKey _PrivateKey;

        internal PkiKeyPair(AsymmetricCipherKeyPair nativeKeyPair,
            PkiAsymmetricAlgorithm algorithm = PkiAsymmetricAlgorithm.Unknown)
        {
            NativeKeyPair = nativeKeyPair;
            Algorithm = algorithm;
        }

        public PkiAsymmetricAlgorithm Algorithm { get; }

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

        /// <summary>
        /// Generates an RSA key pair for the argument bit length.
        /// Some typical bit lengths include 1024, 2048 and 4096.
        /// It is generally agreed upon that modern use of RSA should
        /// require a minimum of 2048-bit key size.
        /// </summary>
        public static PkiKeyPair GenerateRsaKeyPair(int bits)
        {
            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/pkcs/test/PKCS10Test.cs

            var rsaParams = new RsaKeyGenerationParameters(
                    BigInteger.ValueOf(0x10001), new SecureRandom(), bits, 25);
            var rsaKpGen = GeneratorUtilities.GetKeyPairGenerator("RSA");
            rsaKpGen.Init(rsaParams);
            var nativeKeyPair = rsaKpGen.GenerateKeyPair();

            return new PkiKeyPair(nativeKeyPair, PkiAsymmetricAlgorithm.Rsa);
        }

        public static PkiKeyPair GenerateEcdsaKeyPair(int bits)
        {
            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/ECTest.cs#L331
            //    https://www.codeproject.com/Tips/1150485/Csharp-Elliptical-Curve-Cryptography-with-Bouncy-C

            var ecNistParams = NistNamedCurves.GetByName("P-" + bits);
            var ecDomainParams = new ECDomainParameters(ecNistParams.Curve,
                    ecNistParams.G, ecNistParams.N, ecNistParams.H, ecNistParams.GetSeed());
            var ecParams = new ECKeyGenerationParameters(ecDomainParams, new SecureRandom());
            var ecKpGen = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            ecKpGen.Init(ecParams);
            var nativeKeyPair = ecKpGen.GenerateKeyPair();

            return new PkiKeyPair(nativeKeyPair, PkiAsymmetricAlgorithm.Ecdsa);
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
        public class RecoverableSerialForm
        {
            public RecoverableSerialForm()
            { }

            public RecoverableSerialForm(PkiKeyPair keyPair)
            {
                _algorithm = keyPair.Algorithm;
                _privateKey = keyPair.PrivateKey.Export(PkiEncodingFormat.Der);
                _publicKey = keyPair.PublicKey.Export(PkiEncodingFormat.Der);
            }

            public int _ver = 1;
            public PkiAsymmetricAlgorithm _algorithm;
            public byte[] _privateKey;
            public byte[] _publicKey;

            public PkiKeyPair Recover()
            {
                var pubKey = PublicKeyFactory.CreateKey(_publicKey);
                var prvKey = PrivateKeyFactory.CreateKey(_privateKey);

                return new PkiKeyPair(null, _algorithm)
                {
                    _PrivateKey = new PkiKey(prvKey, _algorithm),
                    _PublicKey = new PkiKey(pubKey, _algorithm),
                };
            }
        }
    }
}