using System.Collections;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PKISharp.SimplePKI.UnitTests
{
    [TestClass]
    public class PkiCertificateSigningRequestTests
    {
        private readonly string _testTemp;

        public PkiCertificateSigningRequestTests()
        {
            _testTemp = Path.GetFullPath("_TMP");
            if (!Directory.Exists(_testTemp))
                Directory.CreateDirectory(_testTemp);

        }

        [TestMethod]
        [DataRow(1024, PkiHashAlgorithm.Sha256)]
        [DataRow(2048, PkiHashAlgorithm.Sha256)]
        [DataRow(4096, PkiHashAlgorithm.Sha256)]
        [DataRow(1024, PkiHashAlgorithm.Sha512)]
        [DataRow(2048, PkiHashAlgorithm.Sha512)]
        [DataRow(4096, PkiHashAlgorithm.Sha512)]
        public void CreateAndExportRsaCsr(int bits, PkiHashAlgorithm hashAlgor)
        {
            var sn = "CN=foo.example.com";
            var keys = PkiKeyPair.GenerateRsaKeyPair(bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);

            var pemOut = Path.Combine(_testTemp,
                    $"csr-rsa-{bits}-{hashAlgor}.pem");
            var derOut = Path.Combine(_testTemp,
                    $"csr-rsa-{bits}-{hashAlgor}.der");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);

            File.WriteAllBytes(pemOut, csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, csr.ExportSigningRequest(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"req -text -noout -verify -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"req -text -noout -verify -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(1024, PkiHashAlgorithm.Sha256)]
        [DataRow(2048, PkiHashAlgorithm.Sha256)]
        [DataRow(4096, PkiHashAlgorithm.Sha256)]
        [DataRow(1024, PkiHashAlgorithm.Sha512)]
        [DataRow(2048, PkiHashAlgorithm.Sha512)]
        [DataRow(4096, PkiHashAlgorithm.Sha512)]
        public void CreateAndExportRsaSansCsr(int bits, PkiHashAlgorithm hashAlgor)
        {
            var sn = "CN=foo.example.com";
            var sans = new[] {
                "foo-alt1.example.com",
                "foo-alt2.example.com",
            };
            var keys = PkiKeyPair.GenerateRsaKeyPair(bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);
            csr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(sans));

            var pemOut = Path.Combine(_testTemp,
                    $"csr-rsa-{bits}-{hashAlgor}-sans.pem");
            var derOut = Path.Combine(_testTemp,
                    $"csr-rsa-{bits}-{hashAlgor}-sans.der");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);
            Assert.AreEqual(1, csr.CertificateExtensions.Count);

            File.WriteAllBytes(pemOut, csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, csr.ExportSigningRequest(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"req -text -noout -verify -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"req -text -noout -verify -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(224, PkiHashAlgorithm.Sha256)]
        [DataRow(256, PkiHashAlgorithm.Sha256)]
        [DataRow(384, PkiHashAlgorithm.Sha256)]
        [DataRow(224, PkiHashAlgorithm.Sha512)]
        [DataRow(256, PkiHashAlgorithm.Sha512)]
        [DataRow(384, PkiHashAlgorithm.Sha512)]
        public void CreateAndExportEcdsaCsr(int bits, PkiHashAlgorithm hashAlgor)
        {
            var sn = "CN=foo.example.com";
            var keys = PkiKeyPair.GenerateEcdsaKeyPair(bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);

            var pemOut = Path.Combine(_testTemp,
                    $"csr-ecdsa-{bits}-{hashAlgor}.pem");
            var derOut = Path.Combine(_testTemp,
                    $"csr-ecdsa-{bits}-{hashAlgor}.der");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);

            File.WriteAllBytes(pemOut, csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, csr.ExportSigningRequest(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"req -text -noout -verify -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"req -text -noout -verify -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(224, PkiHashAlgorithm.Sha256)]
        [DataRow(256, PkiHashAlgorithm.Sha256)]
        [DataRow(384, PkiHashAlgorithm.Sha256)]
        [DataRow(224, PkiHashAlgorithm.Sha512)]
        [DataRow(256, PkiHashAlgorithm.Sha512)]
        [DataRow(384, PkiHashAlgorithm.Sha512)]
        public void CreateAndExportEcdsaSansCsr(int bits, PkiHashAlgorithm hashAlgor)
        {
            var sn = "CN=foo.example.com";
            var sans = new[] {
                "foo-alt1.example.com",
                "foo-alt2.example.com",
            };
            var keys = PkiKeyPair.GenerateEcdsaKeyPair(bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);
            csr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(sans));

            var pemOut = Path.Combine(_testTemp,
                    $"csr-ecdsa-{bits}-{hashAlgor}-sans.pem");
            var derOut = Path.Combine(_testTemp,
                    $"csr-ecdsa-{bits}-{hashAlgor}-sans.der");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);
            Assert.AreEqual(1, csr.CertificateExtensions.Count);

            File.WriteAllBytes(pemOut, csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, csr.ExportSigningRequest(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"req -text -noout -verify -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"req -text -noout -verify -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void SaveLoadCsr(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var sn = "CN=foo.example.com";
            var sans = new[] {
                "foo-alt1.example.com",
                "foo-alt2.example.com",
            };
            var keys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);
            csr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(sans));

            var saveOut = Path.Combine(_testTemp,
                    $"csrsave-{algor}-{bits}-{hashAlgor}-sans.ser");

            using (var ms = new MemoryStream())
            {
                csr.Save(ms);
                File.WriteAllBytes(saveOut, ms.ToArray());
            }

            PkiCertificateSigningRequest csr2;
            using (var fs = new FileStream(saveOut, FileMode.Open))
            {
                csr2 = PkiCertificateSigningRequest.Load(fs);
            }

            using (var ms = new MemoryStream())
            {
                csr2.Save(ms);
                File.WriteAllBytes(saveOut + 2, ms.ToArray());
            }

            File.WriteAllBytes(saveOut + "1a.pem", csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(saveOut + "1b.pem", csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(saveOut + "2a.pem", csr2.ExportSigningRequest(PkiEncodingFormat.Pem));
            File.WriteAllBytes(saveOut + "2b.pem", csr2.ExportSigningRequest(PkiEncodingFormat.Pem));

            Assert.AreEqual(csr.SubjectName, csr2.SubjectName);
            Assert.AreEqual(csr.HashAlgorithm, csr2.HashAlgorithm);
            CollectionAssert.AreEqual(
                    csr.PublicKey.Export(PkiEncodingFormat.Der),
                    csr2.PublicKey.Export(PkiEncodingFormat.Der));
            CollectionAssert.AreEqual(
                    csr.CertificateExtensions,
                    csr2.CertificateExtensions, CertExtComparerInstance);

            Assert.AreEqual(csr.PublicKey.IsPrivate, csr2.PublicKey.IsPrivate);
            Assert.AreEqual(csr.PublicKey.Algorithm, csr2.PublicKey.Algorithm);
            CollectionAssert.AreEqual(
                    csr.PublicKey.Export(PkiEncodingFormat.Der),
                    csr2.PublicKey.Export(PkiEncodingFormat.Der));
        }


        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048, PkiEncodingFormat.Der)]
        // [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256, PkiEncodingFormat.Pem)]
        public void ExportImportCsr(PkiAsymmetricAlgorithm algor, int bits, PkiEncodingFormat format)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var sn = "CN=foo.example.com";
            var sans = new[] {
                "foo-alt1.example.com",
                "foo-alt2.example.com",
            };
            var keys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);
            csr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(sans));

            var saveOut = Path.Combine(_testTemp,
                    $"csrexport-{algor}-{bits}-{hashAlgor}-sans.{format}");

            var encoding = csr.ExportSigningRequest(format);
            File.WriteAllBytes(saveOut, encoding);

            encoding = File.ReadAllBytes(saveOut);
            PkiCertificateSigningRequest csr2 = new PkiCertificateSigningRequest(format, encoding, hashAlgor);

            using (var ms = new MemoryStream())
            {
                csr2.Save(ms);
                File.WriteAllBytes(saveOut + 2, ms.ToArray());
            }

            // File.WriteAllBytes(saveOut + "1a.pem", csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            // File.WriteAllBytes(saveOut + "1b.pem", csr.ExportSigningRequest(PkiEncodingFormat.Pem));
            // File.WriteAllBytes(saveOut + "2a.pem", csr2.ExportSigningRequest(PkiEncodingFormat.Pem));
            // File.WriteAllBytes(saveOut + "2b.pem", csr2.ExportSigningRequest(PkiEncodingFormat.Pem));

            Assert.AreEqual(csr.SubjectName, csr2.SubjectName);
            Assert.AreEqual(csr.HashAlgorithm, csr2.HashAlgorithm);
            CollectionAssert.AreEqual(
                    csr.PublicKey.Export(PkiEncodingFormat.Der),
                    csr2.PublicKey.Export(PkiEncodingFormat.Der));
            CollectionAssert.AreEqual(
                    csr.CertificateExtensions,
                    csr2.CertificateExtensions, CertExtComparerInstance);

            Assert.AreEqual(csr.PublicKey.IsPrivate, csr2.PublicKey.IsPrivate);
            Assert.AreEqual(csr.PublicKey.Algorithm, csr2.PublicKey.Algorithm);
            CollectionAssert.AreEqual(
                    csr.PublicKey.Export(PkiEncodingFormat.Der),
                    csr2.PublicKey.Export(PkiEncodingFormat.Der));
        }

        private readonly CertExtComparer CertExtComparerInstance = new CertExtComparer();

        class CertExtComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                var ext1 = (PkiCertificateExtension)x;
                var ext2 = (PkiCertificateExtension)y;
                return ext1.CompareTo(ext2);
            }
        }
    }
}
