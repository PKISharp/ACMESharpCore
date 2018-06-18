using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACMESharp.Crypto.PKI.UnitTests
{
    [TestClass]
    public class PkiKeyTests
    {
        string _testTemp;

        public PkiKeyTests()
        {
            _testTemp = Path.GetFullPath("_TMP");
            if (!Directory.Exists(_testTemp))
                Directory.CreateDirectory(_testTemp);

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
            
            using (var proc = Process.Start("openssl", $"rsa -in {pubOut} -pubin"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = Process.Start("openssl", $"rsa -in {prvOut} -check"))
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

            
            using (var proc = Process.Start("openssl", $"ec -in {pubOut} -pubin"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = Process.Start("openssl", $"ec -in {prvOut} -check"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }
    }
}
