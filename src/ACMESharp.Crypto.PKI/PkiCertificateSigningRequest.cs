using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace ACMESharp.Crypto.PKI
{
    public class PkiCertificateSigningRequest
    {
        private PkiKeyPair _keyPair;

        public PkiCertificateSigningRequest(string subjectName, PkiKeyPair keyPair,
            PkiHashAlgorithm hashAlgorithm)
        {
            SubjectName = subjectName;
            _keyPair = keyPair;
            PublicKey = _keyPair.PublicKey;
            HashAlgorithm = hashAlgorithm;
        }

        public string SubjectName { get; }

        public PkiKey PublicKey { get; }

        public PkiHashAlgorithm HashAlgorithm { get; }

        public Collection<PkiCertificateExtension> CertificateExtensions { get; }
                = new Collection<PkiCertificateExtension>();

        /// <summary>
        /// Creates an ASN.1 DER-encoded PKCS#10 CertificationRequest object representing
        /// the current state of this CertificateRequest object.
        /// </summary>
        /// <returns>An ASN.1 DER-encoded certificate signing request.</returns>
        public byte[] ExportSigningRequest(PkiEncodingFormat format)
        {
            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/pkcs/test/PKCS10Test.cs
            //    https://stackoverflow.com/questions/46182659/how-to-delay-sign-the-certificate-request-using-bouncy-castle-with-ecdsa-signatu
            //    http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation:
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-EllipticCurve(ECDSA)
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-RSA
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-CreatingCertificationRequests
            //    https://stackoverflow.com/a/37563051/5428506


            var hashAlgorName = HashAlgorithm.ToString().ToUpper();
            var asymAlgorName = _keyPair.Algorithm.ToString().ToUpper();
            var sigAlgor = $"{hashAlgorName}with{asymAlgorName}";

            var x509name = new X509Name($"CN={SubjectName}");
            var pubKey = _keyPair.PublicKey.NativeKey;
            var prvKey = _keyPair.PrivateKey.NativeKey;

            Asn1Set attrSet = null;
            if (CertificateExtensions.Count > 0)
            {
                var certExts = CertificateExtensions.ToDictionary(
                        ext => ext.Identifier, ext => ext.Value);
                var csrAttrs = new[]
                {
                    new Org.BouncyCastle.Asn1.Cms.Attribute(
                        PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                        new DerSet(new X509Extensions(certExts))),
                };
                attrSet = new DerSet(csrAttrs);
            }

            var sigFactory = new Asn1SignatureFactory(sigAlgor, prvKey);
            var pkcs10 = new Pkcs10CertificationRequest(sigFactory, x509name,
                    pubKey, attrSet, prvKey);
            
            switch (format)
            {
                case PkiEncodingFormat.Pem:
                    using (var sw = new StringWriter())
                    {
                        var pemWriter = new PemWriter(sw);
                        pemWriter.WriteObject(pkcs10);
                        return Encoding.UTF8.GetBytes(sw.GetStringBuilder().ToString());
                    }
                
                case PkiEncodingFormat.Der:
                    return pkcs10.GetDerEncoded();

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Creates a self-signed certificate using the established subject, key,
        /// and optional extensions.
        /// </summary>
        /// <param name="notBefore">The oldest date and time when this certificate is considered
        ///         valid. Typically UtcNow, plus or minus a few seconds.</param>
        /// <param name="notAfter">The date and time when this certificate is no longer considered
        ///         valid.</param>
        /// <returns>A Certificate with the specified values. The returned object
        ///         will assert HasPrivateKey.</returns>
        public PkiCertificate CreateSelfSigned(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            


            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Creates a certificate using the established subject, key, and optional
        /// extensions using the specified certificate as the issuer.
        /// </summary>
        /// <param name="issuerCertificate">Certificate instance representing the issuing
        ///         Certificate Authority (CA).</param>
        /// <param name="notBefore">The oldest date and time when this certificate is considered
        ///         valid. Typically UtcNow, plus or minus a few seconds.</param>
        /// <param name="notAfter">The date and time when this certificate is no longer considered
        ///         valid.</param>
        /// <param name="serialNumber">The serial number to use for the new certificate.
        ///         This value should be unique per issuer. The value is interpreted as
        ///         an unsigned integer of arbitrary size in big-endian byte ordering.
        ///         RFC 3280 recommends confining it to 20 bytes or less.</param>
        /// <returns>A Certificate with the specified values. The returned object
        ///         won't assert HasPrivateKey.</returns>
        public PkiCertificate Create(PkiCertificate issuerCertificate,
            DateTimeOffset notBefore, DateTimeOffset notAfter, byte[] serialNumber)
        {
            throw new System.NotImplementedException();
        }
    }
}