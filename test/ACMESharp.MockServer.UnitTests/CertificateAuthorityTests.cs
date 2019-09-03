using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.SimplePKI;

namespace ACMESharp.MockServer.UnitTests
{
    [TestClass]
    public class CertificateAuthorityTests
    {
        public const string DataFolder = @".\_IGNORE\data";

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
        }

        [TestMethod]
        public void CsrRsaExportImportDer()
        {
            var kpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var csr1 = new PkiCertificateSigningRequest("cn=foo.mock.acme2.zyborg.io", kpr,
                    PkiHashAlgorithm.Sha256);
            
            var bytes = csr1.ExportSigningRequest(PkiEncodingFormat.Der);
            var csr2 = new PkiCertificateSigningRequest(PkiEncodingFormat.Der, bytes,
                    PkiHashAlgorithm.Sha256);
        }

        [TestMethod]
        public void CsrRsaExportImportPem()
        {
            var kpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var csr1 = new PkiCertificateSigningRequest("cn=foo.mock.acme2.zyborg.io", kpr,
                    PkiHashAlgorithm.Sha256);
            
            var bytes = csr1.ExportSigningRequest(PkiEncodingFormat.Pem);
            var csr2 = new PkiCertificateSigningRequest(PkiEncodingFormat.Pem, bytes,
                    PkiHashAlgorithm.Sha256);
        }

        [TestMethod]
        public void GenCaCertificate()
        {
            var caKpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var caCsr = new PkiCertificateSigningRequest("cn=test-ca",
                    caKpr, PkiHashAlgorithm.Sha256);
            
            var caCrt = caCsr.CreateCa(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddDays(30));
        }

        [TestMethod]
        public void RsaCaCertificateSignsImportedPemCsr()
        {
            var caKpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var caCsr = new PkiCertificateSigningRequest("cn=test-ca",
                    caKpr, PkiHashAlgorithm.Sha256);
            
            var caCrt = caCsr.CreateCa(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddDays(30));

            var kpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var csr1 = new PkiCertificateSigningRequest("cn=foo.mock.acme2.zyborg.io", kpr,
                    PkiHashAlgorithm.Sha256);
            
            var bytes = csr1.ExportSigningRequest(PkiEncodingFormat.Pem);
            var csr2 = new PkiCertificateSigningRequest(PkiEncodingFormat.Pem, bytes,
                    PkiHashAlgorithm.Sha256);
            
            var serNum = DateTime.Now.Ticks;
            var serNumBytes = BitConverter.GetBytes(serNum);
            if (BitConverter.IsLittleEndian)
                serNumBytes = serNumBytes.Reverse().ToArray();

            var crt = csr2.Create(caCrt, caKpr.PrivateKey,
                    DateTimeOffset.Now.AddHours(-1),
                    DateTimeOffset.Now.AddHours(24),
                    serNumBytes);
            
            var crtDer = crt.Export(PkiEncodingFormat.Der);
            var crtPem = crt.Export(PkiEncodingFormat.Pem);

            File.WriteAllBytes(Path.Combine(DataFolder, "pem_imported-rsa_signed.der.crt"), crtDer);
            File.WriteAllBytes(Path.Combine(DataFolder, "pem_imported-rsa_signed.pem.crt"), crtPem);
        }

        [TestMethod]
        public void RsaCaCertificateSignsImportedDerCsr()
        {
            var caKpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var caCsr = new PkiCertificateSigningRequest("cn=test-ca",
                    caKpr, PkiHashAlgorithm.Sha256);
            
            var caCrt = caCsr.CreateCa(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddDays(30));

            var kpr = PkiKeyPair.GenerateRsaKeyPair(2048);
            var csr1 = new PkiCertificateSigningRequest("cn=foo.mock.acme2.zyborg.io", kpr,
                    PkiHashAlgorithm.Sha256);
            
            var bytes = csr1.ExportSigningRequest(PkiEncodingFormat.Der);
            var csr2 = new PkiCertificateSigningRequest(PkiEncodingFormat.Der, bytes,
                    PkiHashAlgorithm.Sha256);
            
            var serNum = DateTime.Now.Ticks;
            var serNumBytes = BitConverter.GetBytes(serNum);
            if (BitConverter.IsLittleEndian)
                serNumBytes = serNumBytes.Reverse().ToArray();

            var crt = csr2.Create(caCrt, caKpr.PrivateKey,
                    DateTimeOffset.Now.AddHours(-1),
                    DateTimeOffset.Now.AddHours(24),
                    serNumBytes);
            
            var crtDer = crt.Export(PkiEncodingFormat.Der);
            var crtPem = crt.Export(PkiEncodingFormat.Pem);

            File.WriteAllBytes(Path.Combine(DataFolder, "der_imported-rsa_signed.der.crt"), crtDer);
            File.WriteAllBytes(Path.Combine(DataFolder, "der_imported-rsa_signed.pem.crt"), crtPem);
        }

        [TestMethod]
        public void EcdsaCaCertificateSignsImportedPemCsr()
        {
            var caKpr = PkiKeyPair.GenerateEcdsaKeyPair(384);
            var caCsr = new PkiCertificateSigningRequest("cn=test-ca",
                    caKpr, PkiHashAlgorithm.Sha256);
            
            var caCrt = caCsr.CreateCa(
                    DateTimeOffset.Now.AddDays(-1),
                    DateTimeOffset.Now.AddDays(30));

            var kpr = PkiKeyPair.GenerateEcdsaKeyPair(384);
            var csr1 = new PkiCertificateSigningRequest("cn=foo.mock.acme2.zyborg.io", kpr,
                    PkiHashAlgorithm.Sha256);
            
            var bytes = csr1.ExportSigningRequest(PkiEncodingFormat.Pem);
            var csr2 = new PkiCertificateSigningRequest(PkiEncodingFormat.Pem, bytes,
                    PkiHashAlgorithm.Sha256);

            csr2.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(new[] {
                        "foo-alt1.mock.acme2.zyborg.io",
                        "foo-alt2.mock.acme2.zyborg.io",
                        "foo-alt3.mock.acme2.zyborg.io",
                    }));

            var serNum = DateTime.Now.Ticks;
            var serNumBytes = BitConverter.GetBytes(serNum);
            if (BitConverter.IsLittleEndian)
                serNumBytes = serNumBytes.Reverse().ToArray();

            var crt = csr2.Create(caCrt, caKpr.PrivateKey,
                    DateTimeOffset.Now.AddHours(-1),
                    DateTimeOffset.Now.AddHours(24),
                    serNumBytes);
            
            var crtDer = crt.Export(PkiEncodingFormat.Der);
            var crtPem = crt.Export(PkiEncodingFormat.Pem);

            File.WriteAllBytes(Path.Combine(DataFolder, "pem_imported-ecdsa_signed.der.crt"), crtDer);
            File.WriteAllBytes(Path.Combine(DataFolder, "pem_imported-ecdsa_signed.pem.crt"), crtPem);
        }
    }
}