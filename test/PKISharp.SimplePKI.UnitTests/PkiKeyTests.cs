using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PKISharp.SimplePKI.UnitTests
{
    [TestClass]
    public class PkiKeyTests
    {
        private readonly string _testTemp;

        public PkiKeyTests()
        {
            _testTemp = Path.GetFullPath("_TMP");
            if (!Directory.Exists(_testTemp))
                Directory.CreateDirectory(_testTemp);

        }

        internal static PkiKeyPair GenerateKeyPair(PkiAsymmetricAlgorithm algor, int bits)
        {
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    return PkiKeyPair.GenerateRsaKeyPair(bits);
                case PkiAsymmetricAlgorithm.Ecdsa:
                    return PkiKeyPair.GenerateEcdsaKeyPair(bits);
                default:
                    throw new NotSupportedException(nameof(PkiAsymmetricAlgorithm));
            }            
        }

        [TestMethod]
        [DataRow(1024)]
        [DataRow(2048)]
        [DataRow(4096)]
        public void CreateRsaKeyPair(int bits)
        {
            var rsaKeys = PkiKeyPair.GenerateRsaKeyPair(bits);
            
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.Algorithm);
            Assert.IsFalse(rsaKeys.PublicKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.PublicKey.Algorithm);
            Assert.IsTrue(rsaKeys.PrivateKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.PrivateKey.Algorithm);

            var pubOut = Path.Combine(_testTemp, $"keypair-rsa-pub{bits}.pem");
            var prvOut = Path.Combine(_testTemp, $"keypair-rsa-prv{bits}.pem");

            File.WriteAllBytes(pubOut, rsaKeys.PublicKey.Export(PkiEncodingFormat.Pem));
            File.WriteAllBytes(prvOut, rsaKeys.PrivateKey.Export(PkiEncodingFormat.Pem));
            
            using (var proc = OpenSsl.Start($"rsa -in {pubOut} -pubin"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"rsa -in {prvOut} -check"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(2048)]
        public void ExportRsaKeyPairWithPassword(int bits)
        {
            var rsaKeys = PkiKeyPair.GenerateRsaKeyPair(bits);
            
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.Algorithm);
            Assert.IsFalse(rsaKeys.PublicKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.PublicKey.Algorithm);
            Assert.IsTrue(rsaKeys.PrivateKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Rsa, rsaKeys.PrivateKey.Algorithm);

            var pubOut = Path.Combine(_testTemp, $"keypair-rsa-pub{bits}-secure.pem");
            var prvOut = Path.Combine(_testTemp, $"keypair-rsa-prv{bits}-secure.pem");

            File.WriteAllBytes(pubOut, rsaKeys.PublicKey.Export(PkiEncodingFormat.Pem,
                    password: "123456".ToCharArray()));
            File.WriteAllBytes(prvOut, rsaKeys.PrivateKey.Export(PkiEncodingFormat.Pem,
                    password: "123456".ToCharArray()));
            
            using (var proc = OpenSsl.Start($"rsa -in {pubOut} -pubin"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"rsa -in {prvOut} -check -passin pass:123456"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(224)]
        [DataRow(256)]
        [DataRow(384)]
        public void CreateEcdsaKeyPair(int bits)
        {
            var ecdsaKeys = PkiKeyPair.GenerateEcdsaKeyPair(bits);

            Assert.AreEqual(PkiAsymmetricAlgorithm.Ecdsa, ecdsaKeys.Algorithm);
            Assert.IsFalse(ecdsaKeys.PublicKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Ecdsa, ecdsaKeys.PublicKey.Algorithm);
            Assert.IsTrue(ecdsaKeys.PrivateKey.IsPrivate);
            Assert.AreEqual(PkiAsymmetricAlgorithm.Ecdsa, ecdsaKeys.PublicKey.Algorithm);

            var pubOut = Path.Combine(_testTemp, $"ecdsa-pub-key-{bits}.pem");
            var prvOut = Path.Combine(_testTemp, $"ecdsa-prv-key-{bits}.pem");

            File.WriteAllBytes(pubOut, ecdsaKeys.PublicKey.Export(PkiEncodingFormat.Pem));
            File.WriteAllBytes(prvOut, ecdsaKeys.PrivateKey.Export(PkiEncodingFormat.Pem));

            
            using (var proc = OpenSsl.Start($"ec -in {pubOut} -pubin"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode, "OpenSSL exit code checking EC public key");
            }

            using (var proc = OpenSsl.Start($"ec -in {prvOut} -check"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode, "OpenSSL exit code checking EC private key");
            }
        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void SaveLoadKeyPair(PkiAsymmetricAlgorithm algor, int bits)
        {
            PkiKeyPair kp;
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    kp = PkiKeyPair.GenerateRsaKeyPair(bits);
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    kp = PkiKeyPair.GenerateEcdsaKeyPair(bits);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var saveOut = Path.Combine(_testTemp, $"keypairsave-{algor}-{bits}.ser");
            using (var ms = new MemoryStream())
            {
                kp.Save(ms);
                File.WriteAllBytes(saveOut, ms.ToArray());
            }

            PkiKeyPair kp2;
            using (var fs = new FileStream(saveOut, FileMode.Open))
            {
                kp2 = PkiKeyPair.Load(fs);
            }

            Assert.AreEqual(kp.Algorithm, kp2.Algorithm);
            Assert.AreEqual(kp.PrivateKey.Algorithm, kp2.PrivateKey.Algorithm);
            Assert.AreEqual(kp.PrivateKey.IsPrivate, kp2.PrivateKey.IsPrivate);
            Assert.AreEqual(kp.PublicKey.Algorithm, kp2.PublicKey.Algorithm);
            Assert.AreEqual(kp.PublicKey.IsPrivate, kp2.PublicKey.IsPrivate);
            CollectionAssert.AreEqual(
                kp.PrivateKey.Export(PkiEncodingFormat.Der),
                kp2.PrivateKey.Export(PkiEncodingFormat.Der));
            CollectionAssert.AreEqual(
                kp.PublicKey.Export(PkiEncodingFormat.Der),
                kp2.PublicKey.Export(PkiEncodingFormat.Der));
        }
    }
}
