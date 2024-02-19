using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PKISharp.SimplePKI.UnitTests
{
    [TestClass]
    public class PkiCertificateTests
    {
        string _testTemp;

        public PkiCertificateTests()
        {
            _testTemp = Path.GetFullPath("_TMP");
            if (!Directory.Exists(_testTemp))
                Directory.CreateDirectory(_testTemp);

        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void ExportSelfSignedCert(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var sn = "CN=foo.example.com";
            var keys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);

            var pemOut = Path.Combine(_testTemp,
                    $"certself-{algor}-{bits}-{hashAlgor}.pem");
            var derOut = Path.Combine(_testTemp,
                    $"certself-{algor}-{bits}-{hashAlgor}.der");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);

            var cert = csr.CreateSelfSigned(
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1));
            
            Assert.AreEqual(sn, cert.SubjectName,
                    "Subject Name on PKI Certificate");

            var bclCert = cert.ToBclCertificate();
            Assert.AreEqual(sn, bclCert.Subject,
                    "Subject Name on BCL Certificate");

            Assert.IsFalse(bclCert.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa: 
                    Assert.IsNull(bclCert.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(bclCert.GetECDsaPrivateKey());
                    break;
                default: 
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }


            File.WriteAllBytes(pemOut, cert.Export(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, cert.Export(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"x509 -text -noout -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"x509 -text -noout -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }


        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void ExportSignedCert(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var isurName = "CN=SelfSigned";
            var isurKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var subjName = "CN=foo.example.com";
            var subjKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var isurCsr = new PkiCertificateSigningRequest(isurName, isurKeys, hashAlgor);
            var subjCsr = new PkiCertificateSigningRequest(subjName, subjKeys, hashAlgor);
            subjCsr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(
                            new[] {
                                "foo-alt1.example.com",
                                "foo-alt2.example.com",
                            }));

            var pemOut = Path.Combine(_testTemp,
                    $"cert-{algor}-{bits}-{hashAlgor}.pem");
            var derOut = Path.Combine(_testTemp,
                    $"cert-{algor}-{bits}-{hashAlgor}.der");

            var isurCert = isurCsr.CreateCa(
                    DateTime.Now.AddMonths(-5),
                    DateTime.Now.AddMonths(5));
            
            Assert.AreEqual(isurName, isurCert.SubjectName,
                    "Issuer Name on Issuer Certificate");

            var subjCert = subjCsr.Create(isurCert, isurKeys.PrivateKey,
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1), new[] { (byte)0x2b });

            Assert.AreEqual(isurName, isurCert.SubjectName,
                    "Subject Name on Subject Certificate");

            File.WriteAllBytes(pemOut, subjCert.Export(PkiEncodingFormat.Pem));
            File.WriteAllBytes(derOut, subjCert.Export(PkiEncodingFormat.Der));

            using (var proc = OpenSsl.Start($"x509 -text -noout -in {pemOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"x509 -text -noout -inform DER -in {derOut}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void ExportSelfSignedPkcs12(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var sn = "CN=foo.example.com";
            var keys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);

            var pfxSansKey = Path.Combine(_testTemp,
                    $"certselfexp-{algor}-{bits}-{hashAlgor}-sanskey.pfx");
            var pfxWithKey = Path.Combine(_testTemp,
                    $"certselfexp-{algor}-{bits}-{hashAlgor}-withkey.pfx");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);

            var cert = csr.CreateSelfSigned(
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1));
            
            Assert.AreEqual(sn, cert.SubjectName,
                    "Subject Name on PKI Certificate");

            var bclCert = cert.ToBclCertificate();
            Assert.AreEqual(sn, bclCert.Subject,
                    "Subject Name on BCL Certificate");

            Assert.IsFalse(bclCert.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNull(bclCert.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(bclCert.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }

            File.WriteAllBytes(pfxSansKey, cert.Export(PkiArchiveFormat.Pkcs12));
            File.WriteAllBytes(pfxWithKey, cert.Export(PkiArchiveFormat.Pkcs12,
                    keys.PrivateKey));

            Console.WriteLine($"pfxSansKey: {pfxSansKey}");
            string openSslCmdPfxNoKey = $"pkcs12 -legacy -info -in {pfxSansKey} -passin pass:";
            Console.WriteLine($"openSslCmdPfxNoKey: {openSslCmdPfxNoKey}");

            using (var proc = OpenSsl.Start(openSslCmdPfxNoKey))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            Console.WriteLine($"pfxWithKey: {pfxWithKey}");
            string openSslCmdPfxWithKey = $"pkcs12 -legacy -info -in {pfxWithKey} -passin pass: -nokeys";
            Console.WriteLine($"openSslCmdPfxWithKey: {openSslCmdPfxWithKey}");

            using (var proc = OpenSsl.Start(openSslCmdPfxWithKey))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            var certSansKey = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(pfxSansKey));
            Assert.IsFalse(certSansKey.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNull(certSansKey.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(certSansKey.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }

            var certWithKey = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(pfxWithKey));
            Assert.IsTrue(certWithKey.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNotNull(certWithKey.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNotNull(certWithKey.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }

        }


        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void ExportSignedPkcs12(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var isurName = "CN=SelfSigned";
            var isurKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var subjName = "CN=foo.example.com";
            var subjKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var isurCsr = new PkiCertificateSigningRequest(isurName, isurKeys, hashAlgor);
            var subjCsr = new PkiCertificateSigningRequest(subjName, subjKeys, hashAlgor);
            subjCsr.CertificateExtensions.Add(
                    PkiCertificateExtension.CreateDnsSubjectAlternativeNames(
                            new[] {
                                "foo-alt1.example.com",
                                "foo-alt2.example.com",
                            }));

            var pfxSansKey = Path.Combine(_testTemp,
                    $"certexp-{algor}-{bits}-{hashAlgor}-sanskey.pfx");
            var pfxWithKey = Path.Combine(_testTemp,
                    $"certexp-{algor}-{bits}-{hashAlgor}-withkey.pfx");

            var isurCert = isurCsr.CreateCa(
                    DateTime.Now.AddMonths(-5),
                    DateTime.Now.AddMonths(5));
            
            Assert.AreEqual(isurName, isurCert.SubjectName,
                    "Issuer Name on Issuer Certificate");

            var subjCert = subjCsr.Create(isurCert, isurKeys.PrivateKey,
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1), new[] { (byte)0x2b });

            Assert.AreEqual(isurName, isurCert.SubjectName,
                    "Subject Name on Subject Certificate");

            File.WriteAllBytes(pfxSansKey, subjCert.Export(PkiArchiveFormat.Pkcs12,
                    chain: new[] { isurCert }));
            File.WriteAllBytes(pfxWithKey, subjCert.Export(PkiArchiveFormat.Pkcs12,
                    subjKeys.PrivateKey, new[] { isurCert }));

            using (var proc = OpenSsl.Start($"pkcs12 -legacy -info -in {pfxSansKey} -passin pass:"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            using (var proc = OpenSsl.Start($"pkcs12 -legacy -info -in {pfxWithKey} -passin pass: -nokeys"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            var certSansKey = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(pfxSansKey));
            Assert.IsFalse(certSansKey.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNull(certSansKey.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(certSansKey.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }
            var certWithKey = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(pfxWithKey));
            Assert.IsTrue(certWithKey.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNotNull(certWithKey.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNotNull(certWithKey.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }
        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void ExportSelfSignedPemChain(PkiAsymmetricAlgorithm algor, int bits)
        {
            var hashAlgor = PkiHashAlgorithm.Sha256;

            var sn = "CN=foo.example.com";
            var keys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var csr = new PkiCertificateSigningRequest(sn, keys, hashAlgor);

            var pemSansKey = Path.Combine(_testTemp,
                    $"certselfexp-{algor}-{bits}-{hashAlgor}-sanskey.pem");
            var pemWithKey = Path.Combine(_testTemp,
                    $"certselfexp-{algor}-{bits}-{hashAlgor}-withkey.pem");

            Assert.AreEqual(sn, csr.SubjectName);
            Assert.AreSame(keys.PublicKey, keys.PublicKey);
            Assert.AreEqual(hashAlgor, csr.HashAlgorithm);

            var cert = csr.CreateSelfSigned(
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1));
            
            Assert.AreEqual(sn, cert.SubjectName,
                    "Subject Name on PKI Certificate");

            var bclCert = cert.ToBclCertificate();
            Assert.AreEqual(sn, bclCert.Subject,
                    "Subject Name on BCL Certificate");

            Assert.IsFalse(bclCert.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNull(bclCert.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(bclCert.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }

            File.WriteAllBytes(pemSansKey, cert.Export(PkiArchiveFormat.Pem));
            File.WriteAllBytes(pemWithKey, cert.Export(PkiArchiveFormat.Pem,
                    keys.PrivateKey));

            // Check Cert
            using (var proc = OpenSsl.Start($"x509 -text -noout -in {pemSansKey}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }

            // Check Cert
            using (var proc = OpenSsl.Start($"x509 -text -noout -in {pemWithKey}"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }
            // Check Private Key
            var opensslCmd = "rsa";
            if (algor == PkiAsymmetricAlgorithm.Ecdsa)
                opensslCmd = "ec";
            using (var proc = OpenSsl.Start($"{opensslCmd} -in {pemWithKey} -check"))
            {
                proc.WaitForExit();
                Assert.AreEqual(0, proc.ExitCode);
            }


            var certSansKey = new System.Security.Cryptography.X509Certificates.X509Certificate2(File.ReadAllBytes(pemSansKey));
            Assert.IsFalse(certSansKey.HasPrivateKey);
            switch (algor)
            {
                case PkiAsymmetricAlgorithm.Rsa:
                    Assert.IsNull(certSansKey.GetRSAPrivateKey());
                    break;
                case PkiAsymmetricAlgorithm.Ecdsa:
                    Assert.IsNull(certSansKey.GetECDsaPrivateKey());
                    break;
                default:
                    Assert.Fail($"Add private key check for {algor} ");
                    break;
            }
        }

        [TestMethod]
        [DataRow(PkiAsymmetricAlgorithm.Rsa, 2048)]
        [DataRow(PkiAsymmetricAlgorithm.Ecdsa, 256)]
        public void SaveLoadCertificate(PkiAsymmetricAlgorithm algor, int bits)
        {
             var hashAlgor = PkiHashAlgorithm.Sha256;

            var isurName = "CN=SelfSigned";
            var isurKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var subjName = "CN=foo.example.com";
            var subjKeys = PkiKeyTests.GenerateKeyPair(algor, bits);
            var isurCsr = new PkiCertificateSigningRequest(isurName, isurKeys, hashAlgor);
            var subjCsr = new PkiCertificateSigningRequest(subjName, subjKeys, hashAlgor);
            subjCsr.CertificateExtensions.Add(PkiCertificateExtension.CreateDnsSubjectAlternativeNames(
                new[] {
                    "foo-alt1.example.com",
                    "foo-alt2.example.com",
                }
            ));

            var selfOut = Path.Combine(_testTemp,
                    $"certsave-{algor}-{bits}-{hashAlgor}-self.ser");
            var signedOut = Path.Combine(_testTemp,
                    $"certsave-{algor}-{bits}-{hashAlgor}-signed.ser");

            var isurCert = isurCsr.CreateCa(
                    DateTime.Now.AddMonths(-5),
                    DateTime.Now.AddMonths(5));

            var subjCert = subjCsr.Create(isurCert, isurKeys.PrivateKey,
                    DateTime.Now.AddMonths(-1),
                    DateTime.Now.AddMonths(1), new[] { (byte)0x2b });

            using (var ms = new MemoryStream())
            {
                isurCert.Save(ms);
                File.WriteAllBytes(selfOut, ms.ToArray());
            }
            using (var ms = new MemoryStream())
            {
                subjCert.Save(ms);
                File.WriteAllBytes(signedOut, ms.ToArray());
            }

            PkiCertificate isurCert2;
            PkiCertificate subjCert2;
            using (var fs = new FileStream(selfOut, FileMode.Open))
                isurCert2 = PkiCertificate.Load(fs);
            using (var fs = new FileStream(signedOut, FileMode.Open))
                subjCert2 = PkiCertificate.Load(fs);
            
            var bclIsur = isurCert.ToBclCertificate();
            var bclSubj = subjCert.ToBclCertificate();
            var bclIsur2 = isurCert2.ToBclCertificate();
            var bclSubj2 = subjCert2.ToBclCertificate();

            Assert.AreEqual(bclIsur.GetSerialNumberString(),
                    bclIsur2.GetSerialNumberString(), "Issuer Serial Number");
            Assert.AreEqual(bclIsur.GetCertHashString(),
                    bclIsur2.GetCertHashString(), "Issuer Hash");
            Assert.AreEqual(bclIsur.GetRawCertDataString(),
                    bclIsur2.GetRawCertDataString(), "Issuer Raw Data");

            Assert.AreEqual(bclSubj.GetSerialNumberString(),
                    bclSubj2.GetSerialNumberString(), "Subject Serial Number");
            Assert.AreEqual(bclSubj.GetCertHashString(),
                    bclSubj2.GetCertHashString(), "Subject Hash");
            Assert.AreEqual(bclSubj.GetRawCertDataString(),
                    bclSubj2.GetRawCertDataString(), "Subject Raw Data");
       }
    }
}
