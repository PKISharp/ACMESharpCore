using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SysHashAlgorName = System.Security.Cryptography.HashAlgorithmName;
using System.Text;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using BcCertificate = Org.BouncyCastle.X509.X509Certificate;
using Org.BouncyCastle.Asn1;
using System.Linq;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Utilities;

namespace ACMEKestrel.Crypto
{
    /// <summary>
    /// Wrapper class around a native BouncyCastle Asymmetric Key Pair.
    /// </summary>
    public class CertPrivateKey
    {
        public AsymmetricCipherKeyPair KeyPair { get; set; }
    }

    /// <summary>
    /// Collection of static routines for working with basic entities needs to
    /// support X509 Certificate operations, including request generation,
    /// private key management, standards-based serialization and export.
    /// </summary>
    /// <remarks>
    /// Unfortunately there is not yet enough <i>out-of-the-box</i> support for
    /// general certificate management in .NET Standard, so we rely on a 3rd-party
    /// library to handle most of this work for us, in this case the very capable
    /// BouncyCastle C# library.
    /// </remarks>
    public static class CertHelper
    {
        // Useful references and examples for BC:
        //  CSR:
        //    http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation
        //    https://gist.github.com/Venomed/5337717aadfb61b09e58
        //    http://codereview.stackexchange.com/questions/84752/net-bouncycastle-csr-and-private-key-generation
        //  Other:
        //    https://www.txedo.com/blog/java-read-rsa-keys-pem-file/

        public const int RSA_BITS_DEFAULT = 2048;
        public const int RSA_BITS_MINIMUM = 1024 + 1; // LE no longer allows 1024-bit PrvKeys

        public static readonly BigInteger RSA_E_3 = BigInteger.Three;
        public static readonly BigInteger RSA_E_F4 = BigInteger.ValueOf(0x10001);

        // This is based on the BC RSA Generator code:
        //    https://github.com/bcgit/bc-csharp/blob/fba5af528ce7dcd0ac0513363196a62639b82a86/crypto/src/crypto/generators/RsaKeyPairGenerator.cs#L37
        const int DEFAULT_CERTAINTY = 100;

        public static CertPrivateKey GenerateRsaPrivateKey(int bits, string PubExp = null)
        {
            // Bits less than 1024 are weak Ref: http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html
            if (bits < RSA_BITS_MINIMUM)
                bits = RSA_BITS_DEFAULT;

            BigInteger e;
            if (string.IsNullOrEmpty(PubExp))
                e = RSA_E_F4;
            else if (PubExp.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                e = new BigInteger(PubExp, 16);
            else
                e = new BigInteger(PubExp);

            var rsaKgp = new RsaKeyGenerationParameters(
                    e, new SecureRandom(), bits, DEFAULT_CERTAINTY);
            var rkpg = new RsaKeyPairGenerator();
            rkpg.Init(rsaKgp);
            AsymmetricCipherKeyPair ackp = rkpg.GenerateKeyPair();

            return new CertPrivateKey
            {
                KeyPair = ackp,
            };
        }

        public static CertPrivateKey GenerateEcPrivateKey(int bits)
        {
            // From:
            //  http://www.bouncycastle.org/wiki/display/JA1/Elliptic+Curve+Key+Pair+Generation+and+Key+Factories
            // var csr = new Pkcs10CertificationRequest()
            // string curveName;
            // switch (bits)
            // {
            //     case 256:
            //         curveName = "P-256";
            //         break;
            //     case 384:
            //         curveName = "P-384";
            //         break;
            //     default:
            //         throw new ArgumentException("bit length is unsupported or unknown", nameof(bits));

            // }
            // var ecParamSpec = ECNamedCurveTable.GetByName(curveName);
 
            // From:
            //    https://www.codeproject.com/Tips/1150485/Csharp-Elliptical-Curve-Cryptography-with-Bouncy-C
            var ecKpg = new ECKeyPairGenerator("ECDSA");
            ecKpg.Init(new KeyGenerationParameters(new SecureRandom(), bits));
            var ecKp = ecKpg.GenerateKeyPair();

            return new CertPrivateKey
            {
                KeyPair = ecKp
            };
        }        


        public static void ExportPrivateKey(CertPrivateKey pk, EncodingFormat fmt, Stream target)
        {
            switch (fmt)
            {
                case EncodingFormat.PEM:
                    var pem = ToPrivatePem(pk.KeyPair);
                    var bytes = Encoding.UTF8.GetBytes(pem);
                    target.Write(bytes, 0, bytes.Length);
                    break;

                case EncodingFormat.DER:
                    var der = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pk.KeyPair.Private).GetDerEncoded();
                    target.Write(der, 0, der.Length);
                    break;

                default:
                    throw new NotSupportedException("unsupported encoding format");
            }
        }

        public static CertPrivateKey ImportPrivateKey(EncodingFormat fmt, Stream source)
        {
            if (fmt != EncodingFormat.PEM)
                throw new NotSupportedException("Unsupported archive format");

            using (var tr = new StreamReader(source))
            {
                var pr = new PemReader(tr);
                var pem = pr.ReadObject();
                var ackp = pem as AsymmetricCipherKeyPair;

                if (ackp != null)
                {
                    var rsa = ackp.Private as RsaPrivateCrtKeyParameters;
                    if (rsa != null)
                    {
                        return new CertPrivateKey
                        {
                            KeyPair = ackp,
                        };
                    }
                }

                throw new InvalidDataException("could not read source as PEM private key");
            }
        }

        public static byte[] GenerateRsaCsr(IEnumerable<string> names,
                CertPrivateKey pk, SysHashAlgorName? hashAlgor = null)
        {
            if (hashAlgor == null)
                hashAlgor = SysHashAlgorName.SHA256;

            var attrs = new Dictionary<DerObjectIdentifier, string>
            {
                [X509Name.CN] = names.First(),
            };
            var subj = new X509Name(attrs.Keys.ToList(), attrs.Values.ToList());

            var ackp = pk.KeyPair;

            var sigAlg = $"{hashAlgor.Value.Name}withRSA";
            var csrAttrs = new List<Asn1Encodable>();

            var gnames = new List<GeneralName>(
                    names.Select(x => new GeneralName(GeneralName.DnsName, x)));

            var altNames = new GeneralNames(gnames.ToArray());
#pragma warning disable CS0612 // Type or member is obsolete
            var x509Ext = new X509Extensions(new Hashtable
            {
                [X509Extensions.SubjectAlternativeName] = new X509Extension(
                        false, new DerOctetString(altNames))
            });
#pragma warning restore CS0612 // Type or member is obsolete

            csrAttrs.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                    PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                    new DerSet(x509Ext)));

#pragma warning disable CS0618 // Type or member is obsolete
            var csr = new Pkcs10CertificationRequest(sigAlg,
                    subj, ackp.Public, new DerSet(csrAttrs.ToArray()), ackp.Private);
#pragma warning restore CS0618 // Type or member is obsolete

            return csr.GetDerEncoded();
        }


        public static byte[] GenerateEcCsr(IEnumerable<string> names,
                CertPrivateKey pk, SysHashAlgorName? hashAlgor = null)
        {
            if (hashAlgor == null)
                hashAlgor = SysHashAlgorName.SHA256;

            var attrs = new Dictionary<DerObjectIdentifier, string>
            {
                [X509Name.CN] = names.First(),
            };
            var subj = new X509Name(attrs.Keys.ToList(), attrs.Values.ToList());

            var ackp = pk.KeyPair;

            var sigAlg = $"{hashAlgor.Value.Name}withECDSA";
            var csrAttrs = new List<Asn1Encodable>();

            var gnames = new List<GeneralName>(
                    names.Select(x => new GeneralName(GeneralName.DnsName, x)));

            var altNames = new GeneralNames(gnames.ToArray());
#pragma warning disable CS0612 // Type or member is obsolete
            var x509Ext = new X509Extensions(new Hashtable
            {
                [X509Extensions.SubjectAlternativeName] = new X509Extension(
                        false, new DerOctetString(altNames))
            });
#pragma warning restore CS0612 // Type or member is obsolete

            csrAttrs.Add(new Org.BouncyCastle.Asn1.Cms.Attribute(
                    PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                    new DerSet(x509Ext)));

#pragma warning disable CS0618 // Type or member is obsolete
            var csr = new Pkcs10CertificationRequest(sigAlg,
                    subj, ackp.Public, new DerSet(csrAttrs.ToArray()), ackp.Private);
#pragma warning restore CS0618 // Type or member is obsolete

            return csr.GetDerEncoded();
        }

        public static (CertPrivateKey, BcCertificate) GenerateRsaCACertificate(string subjectName, int keyStrength = 2048)
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
#pragma warning disable CS0618 // Type or member is obsolete
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);
#pragma warning restore CS0618 // Type or member is obsolete

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = subjectDN;
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.Date;
            var notAfter = notBefore.AddYears(2);

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
#pragma warning disable CS0618 // Type or member is obsolete
            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);
#pragma warning restore CS0618 // Type or member is obsolete

            // var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificate.GetEncoded());
            // // Add CA certificate to Root store
            // addCertToStore(cert, StoreName.Root, StoreLocation.CurrentUser);

            var key = new CertPrivateKey
            {
                KeyPair = issuerKeyPair,
            };

            return (key, certificate);
        }

        public static (CertPrivateKey, BcCertificate) GenerateRsaSelfSignedCertificate(string subjectName, string issuerName,
                AsymmetricKeyParameter issuerPrivKey,  int keyStrength = 2048)
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            const string signatureAlgorithm = "SHA256WithRSA";
#pragma warning disable CS0618 // Type or member is obsolete
            certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);
#pragma warning restore CS0618 // Type or member is obsolete

            // Issuer and Subject Name
            var subjectDN = new X509Name(subjectName);
            var issuerDN = new X509Name(issuerName);
            certificateGenerator.SetIssuerDN(issuerDN);
            certificateGenerator.SetSubjectDN(subjectDN);

            // Valid For
            var notBefore = DateTime.UtcNow.AddMonths(-1).Date;
            var notAfter = DateTime.UtcNow.AddMonths(1).Date;

            certificateGenerator.SetNotBefore(notBefore);
            certificateGenerator.SetNotAfter(notAfter);

            // Subject Public Key
            AsymmetricCipherKeyPair subjectKeyPair;
            var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            subjectKeyPair = keyPairGenerator.GenerateKeyPair();

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // Generating the Certificate
            var issuerKeyPair = subjectKeyPair;

            // selfsign certificate
#pragma warning disable CS0618 // Type or member is obsolete
            var certificate = certificateGenerator.Generate(issuerPrivKey, random);
#pragma warning restore CS0618 // Type or member is obsolete

            var key = new CertPrivateKey
            {
                KeyPair = subjectKeyPair,
            };
            return (key, certificate);
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate2
            ToDotNetCert(CertPrivateKey key, BcCertificate certificate)
        {
            // merge into X509Certificate2

            // correcponding private key
            PrivateKeyInfo info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(key.KeyPair.Private);

            var x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    certificate.GetEncoded());

#pragma warning disable CS0618 // Type or member is obsolete
            var seq = (Asn1Sequence)Asn1Object.FromByteArray(info.PrivateKey.GetDerEncoded());
#pragma warning restore CS0618 // Type or member is obsolete
            if (seq.Count != 9)
                throw new PemException("malformed sequence in RSA private key");

#pragma warning disable CS0618 // Type or member is obsolete
            var rsa = new RsaPrivateKeyStructure(seq);
#pragma warning restore CS0618 // Type or member is obsolete
            RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent,
                rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

            x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
            return x509;
        }

        public static BcCertificate ImportCertificate(EncodingFormat fmt, Stream source)
        {
            X509Certificate bcCert = null;

            if (fmt == EncodingFormat.DER)
            {
                var certParser = new X509CertificateParser();
                bcCert = certParser.ReadCertificate(source);
            }
            else if (fmt == EncodingFormat.PEM)
            {
                using (var tr = new StreamReader(source))
                {
                    var pr = new PemReader(tr);
                    bcCert = (X509Certificate)pr.ReadObject();
                }
            }
            else
            {
                throw new NotSupportedException("encoding format has not been implemented");
            }

            return bcCert;
        }

        public static void ExportCertificate(BcCertificate cert, EncodingFormat fmt, Stream target)
        {
            if (fmt == EncodingFormat.PEM)
            {
                using (var tw = new StringWriter())
                {
                    var pw = new PemWriter(tw);
                    pw.WriteObject(cert);
                    var pemBytes = Encoding.UTF8.GetBytes(tw.GetStringBuilder().ToString());
                    target.Write(pemBytes, 0, pemBytes.Length);
                }
            }
            else if (fmt == EncodingFormat.DER)
            {
                var der = cert.GetEncoded();
                target.Write(der, 0, der.Length);
            }
            else
            {
                throw new NotSupportedException("unsupported encoding format");
            }
        }

        public static void ExportArchive(CertPrivateKey pk, IEnumerable<BcCertificate> certs,
            ArchiveFormat fmt, Stream target, string password = null)
        {
            if (fmt == ArchiveFormat.PKCS12)
            {
                var bcCerts = certs.Select(x =>
                        new X509CertificateEntry(x)).ToArray();
                var pfx = new Pkcs12Store();
				pfx.SetCertificateEntry(bcCerts[0].Certificate.SubjectDN.ToString(), bcCerts[0]);
				pfx.SetKeyEntry(bcCerts[0].Certificate.SubjectDN.ToString(),
						new AsymmetricKeyEntry(pk.KeyPair.Private), new[] { bcCerts[0] });

                for (int i = 1; i < bcCerts.Length; ++i)
                {
					//pfx.SetCertificateEntry(bcCerts[i].Certificate.SubjectDN.ToString(),
					pfx.SetCertificateEntry(i.ToString(), bcCerts[i]);
                }

				// It used to be pretty straight forward to export this...
				pfx.Save(target, password?.ToCharArray(), new SecureRandom());
			}
            else
            {
                throw new NotSupportedException("unsupported archive format");
            }
        }

        private static string ToPrivatePem(AsymmetricCipherKeyPair ackp)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new PemWriter(tw);
                pw.WriteObject(ackp.Private);
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }

            return pem;
        }
    }
}