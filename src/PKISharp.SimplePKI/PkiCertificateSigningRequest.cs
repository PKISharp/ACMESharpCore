using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace PKISharp.SimplePKI
{
    public class PkiCertificateSigningRequest
    {
        private PkiKeyPair _keyPair;

        /// <summary>
        /// Creates a new instance of a PKI Certificate Signing Request.
        /// </summary>
        /// <param name="subjectName">The Subject Name of the Certificate Request in X509
        ///         directory format, e.g. <c>CN=app.example.com</c>.</param>
        /// <param name="keyPair">A public/private key pair.</param>
        /// <param name="hashAlgorithm">The hash algorithm to be used.</param>
        public PkiCertificateSigningRequest(string subjectName, PkiKeyPair keyPair,
            PkiHashAlgorithm hashAlgorithm)
        {
            SubjectName = subjectName;
            _keyPair = keyPair;
            PublicKey = _keyPair.PublicKey;
            HashAlgorithm = hashAlgorithm;
        }

        public PkiCertificateSigningRequest(PkiEncodingFormat format, byte[] encoded,
            PkiHashAlgorithm hashAlgorithm)
        {
            Pkcs10CertificationRequest pkcs10;
            switch (format)
            {
                case PkiEncodingFormat.Pem:
                    var encodedString = Encoding.UTF8.GetString(encoded);
                    using (var sr = new StringReader(encodedString))
                    {
                        var pemReader = new PemReader(sr);
                        pkcs10 = pemReader.ReadObject() as Pkcs10CertificationRequest;
                        if (pkcs10 == null)
                            throw new Exception("invalid PEM object is not PKCS#10 archive");
                    }
                    break;
                case PkiEncodingFormat.Der:
                    pkcs10 = new Pkcs10CertificationRequest(encoded);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var info = pkcs10.GetCertificationRequestInfo();
            var nativePublicKey = pkcs10.GetPublicKey();
            var rsaKey = nativePublicKey as RsaKeyParameters;
            var ecdsaKey = nativePublicKey as ECPublicKeyParameters;

            if (rsaKey != null)
            {
                PublicKey = new PkiKey(nativePublicKey, PkiAsymmetricAlgorithm.Rsa);
            }
            else if (ecdsaKey != null)
            {
                PublicKey = new PkiKey(nativePublicKey, PkiAsymmetricAlgorithm.Ecdsa);
            }
            else
            {
                throw new NotSupportedException("unsupported asymmetric algorithm key");
            }
            SubjectName = info.Subject.ToString();
            HashAlgorithm = hashAlgorithm;


            // // // Based on:
            // // //    http://forum.rebex.net/4284/pkcs10-certificate-request-example-provided-castle-working

            // // var extGen = new X509ExtensionsGenerator();
            // // foreach (var ext in CertificateExtensions)
            // // {
            // //     extGen.AddExtension(ext.Identifier, ext.IsCritical, ext.Value);
            // // }
            // // var attr = new AttributeX509(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
            // //         new DerSet(extGen.Generate()));

            
            // Based on:
            //    http://unitstep.net/blog/2008/10/27/extracting-x509-extensions-from-a-csr-using-the-bouncy-castle-apis/
            //    https://stackoverflow.com/q/24448909/5428506
            foreach (var attr in info.Attributes.ToArray())
            {
                if (attr is DerSequence derSeq && derSeq.Count == 2)
                {
                    var attrX509 = AttributeX509.GetInstance(attr);
                    if (object.Equals(attrX509.AttrType, PkcsObjectIdentifiers.Pkcs9AtExtensionRequest))
                    {
                        // The `Extension Request` attribute is present.
                        // The X509Extensions are contained as a value of the ASN.1 Set.
                        // Assume that it is the first value of the set.
                        if (attrX509.AttrValues.Count >= 1)
                        {
                            var csrExts = X509Extensions.GetInstance(attrX509.AttrValues[0]);
                            foreach (var extOid in csrExts.GetExtensionOids())
                            {
                                if (object.Equals(extOid, X509Extensions.SubjectAlternativeName))
                                {
                                    var ext = csrExts.GetExtension(extOid);
                                    var extVal = ext.Value;
                                    var der = extVal.GetDerEncoded();
                                    // The ext value, which is an ASN.1 Octet String, **MIGHT** be tagged with
                                    // a leading indicator that it's an Octet String and its length, so we want
                                    // to remove it if that's the case to extract the GeneralNames collection
                                    if (der.Length > 2 && der[0] == 4 && der[1] == der.Length - 2)
                                        der = der.Skip(2).ToArray();
                                    var asn1obj = Asn1Object.FromByteArray(der);
                                    var gnames = GeneralNames.GetInstance(asn1obj);
                                    CertificateExtensions.Add(new PkiCertificateExtension
                                    {
                                        Identifier = extOid,
                                        IsCritical = ext.IsCritical,
                                        Value = gnames,
                                    });
                                }
                            }

                            // No need to search any more.
                            break;
                        }                        
                    }
                }
            }
        }

        public string SubjectName { get; }

        public PkiKey PublicKey { get; }

        public bool HasPrivateKey => _keyPair != null;

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
            if (!HasPrivateKey)
                throw new InvalidOperationException("cannot export CSR without a private key");

            // Based on:
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/pkcs/test/PKCS10Test.cs
            //    https://stackoverflow.com/questions/46182659/how-to-delay-sign-the-certificate-request-using-bouncy-castle-with-ecdsa-signatu
            //    http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation:
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-EllipticCurve(ECDSA)
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-RSA
            //        #X.509PublicKeyCertificateandCertificationRequestGeneration-CreatingCertificationRequests
            //    https://stackoverflow.com/a/37563051/5428506

            var x509name = new X509Name(SubjectName);
            var pubKey = _keyPair.PublicKey.NativeKey;
            var prvKey = _keyPair.PrivateKey.NativeKey;

            // Asn1Set attrSet = null;
            // if (CertificateExtensions.Count > 0)
            // {
            //     var certExts = CertificateExtensions.ToDictionary(
            //             ext => ext.Identifier, ext => ext.Value);
            //     var csrAttrs = new[]
            //     {
            //         new Org.BouncyCastle.Asn1.Cms.Attribute(
            //             PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
            //             new DerSet(new X509Extensions(certExts))),
            //     };
            //     attrSet = new DerSet(csrAttrs);
            // }

            // Based on:
            //    http://forum.rebex.net/4284/pkcs10-certificate-request-example-provided-castle-working

            var extGen = new X509ExtensionsGenerator();
            foreach (var ext in CertificateExtensions)
            {
                extGen.AddExtension(ext.Identifier, ext.IsCritical, ext.Value);
            }
            var attr = new AttributeX509(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest,
                    new DerSet(extGen.Generate()));

            var sigFactory = ComputeSignatureAlgorithm(prvKey);
            var pkcs10 = new Pkcs10CertificationRequest(sigFactory, x509name,
                    pubKey, new DerSet(attr));

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
            var name = new X509Name(SubjectName);
            var snum = Org.BouncyCastle.Utilities.BigIntegers.CreateRandomInRange(
                            BigInteger.One, BigInteger.ValueOf(long.MaxValue),
                            new SecureRandom()).ToByteArrayUnsigned();
            return Create(name, _keyPair.PrivateKey, name, notBefore, notAfter, snum);
        }

        public PkiCertificate CreateCa(DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            var name = new X509Name(SubjectName);
            var snumInt = Org.BouncyCastle.Utilities.BigIntegers.CreateRandomInRange(
                            BigInteger.One, BigInteger.ValueOf(long.MaxValue),
                            new SecureRandom());

            // TODO: for some reason, on Linux this was returning negative???
            if (snumInt.SignValue <= 0)
                snumInt = snumInt.Negate();

            var snum = snumInt.ToByteArrayUnsigned();

            // Key Usage:
            //    Digital Signature, Certificate Signing, Off-line CRL Signing, CRL Signing (86)
            
            return Create(name, _keyPair.PrivateKey, name, notBefore, notAfter, snum,
                    new X509KeyUsage(
                            X509KeyUsage.DigitalSignature |
                            X509KeyUsage.KeyCertSign |
                            X509KeyUsage.CrlSign));
        }

        /// <summary>
        /// Creates a certificate using the established subject, key, and optional
        /// extensions using the specified certificate as the issuer.
        /// </summary>
        /// <param name="issuerCertificate">Certificate instance representing the issuing
        ///         Certificate Authority (CA).</param>
        /// <param name="issuerPrivatekey">Key representing the private key of the issuing
        ///         certificate authority.
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
        public PkiCertificate Create(PkiCertificate issuerCertificate, PkiKey issuerPrivateKey,
            DateTimeOffset notBefore, DateTimeOffset notAfter, byte[] serialNumber)
        {
            var isur = new X509Name(issuerCertificate.SubjectName);
            var name = new X509Name(SubjectName);
            return Create(isur, issuerPrivateKey, name, notBefore, notAfter, serialNumber);
        }

        internal ISignatureFactory ComputeSignatureAlgorithm(AsymmetricKeyParameter privateKey)
        {
            var hashAlgorName = HashAlgorithm.ToString().ToUpper();
            var asymAlgorName = (_keyPair?.Algorithm ?? PublicKey.Algorithm).ToString().ToUpper();
            var sigAlgor = $"{hashAlgorName}with{asymAlgorName}";
            var sigFactory = new Asn1SignatureFactory(sigAlgor, privateKey);

            return sigFactory;
        }

        internal PkiCertificate Create(X509Name issuerName, PkiKey issuerPrivateKey,
            X509Name subjectName, DateTimeOffset notBefore, DateTimeOffset notAfter,
            byte[] serialNumber, X509KeyUsage keyUsage = null, KeyPurposeID[] extKeyUsage = null)
        {
            // Based on:
            //    https://stackoverflow.com/a/39456955/5428506
            //    https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/test/CertTest.cs

            var pubKey = _keyPair?.PublicKey.NativeKey ?? PublicKey.NativeKey;

            var sigFactory = ComputeSignatureAlgorithm(issuerPrivateKey.NativeKey);
            var certGen = new X509V3CertificateGenerator();
            certGen.SetSerialNumber(new BigInteger(serialNumber));
            certGen.SetIssuerDN(issuerName);
            certGen.SetSubjectDN(subjectName);
            certGen.SetNotBefore(notBefore.UtcDateTime);
            certGen.SetNotAfter(notAfter.UtcDateTime);
            certGen.SetPublicKey(pubKey);

            if (keyUsage == null)
                keyUsage = new X509KeyUsage(X509KeyUsage.KeyEncipherment |
                        X509KeyUsage.DigitalSignature);
            if (extKeyUsage == null)
                extKeyUsage = [
                    KeyPurposeID.IdKPClientAuth,
                    KeyPurposeID.IdKPServerAuth
                ];
            
            certGen.AddExtension("2.5.29.15", true, keyUsage);
            certGen.AddExtension("2.5.29.37", true, new DerSequence(extKeyUsage));

            // Based on:
            //    https://boredwookie.net/blog/bouncy-castle-add-a-subject-alternative-name-when-creating-a-cer

            foreach (var ext in CertificateExtensions)
            {
                // certGen.AddExtension(ext.Identifier, ext.Value.IsCritical, ext.Value.Value);
                certGen.AddExtension(ext.Identifier, ext.IsCritical, ext.Value);
            }

            var bcCert = certGen.Generate(sigFactory);

            return new PkiCertificate
            {
                NativeCertificate = bcCert,
            };

            // Compare to LE-issued Certs:
            //    Enhanced Key Usage:
            //        Server Authentication (1.3.6.1.5.5.7.3.1)
            //        Client Authentication (1.3.6.1.5.5.7.3.2)
            //    Subject Key Identifier:
            //        e05bf2ba81d8d3845ff45b5638551e64ca19133d
            //    Authority Key Identifier:
            //        KeyID=a84a6a63047dddbae6d139b7a64565eff3a8eca1
            //    Authority Information Access:
            //        [1]Authority Info Access
            //            Access Method=On-line Certificate Status Protocol (1.3.6.1.5.5.7.48.1)
            //            Alternative Name:
            //                URL=http://ocsp.int-x3.letsencrypt.org
            //        [2]Authority Info Access
            //            Access Method=Certification Authority Issuer (1.3.6.1.5.5.7.48.2)
            //            Alternative Name:
            //                URL=http://cert.int-x3.letsencrypt.org/
            //    Certificate Policies:
            //        ...
            //    SCT List:
            //        ...
            //    Key Usage:
            //        Digital Signature, Key Encipherment (a0)
            //    Basic Constraints:
            //        Subject Type=End Entity
            //        Path Length Constraint=None

            // CA:
            //    Key Usage:
            //        Digital Signature, Certificate Signing, Off-line CRL Signing, CRL Signing (86)
        }

        /// <summary>
        /// Saves this CSR instance to the target stream,
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
        /// Recovers a serialized CSR previously saved using
        /// a recoverable serialization format.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static PkiCertificateSigningRequest Load(Stream stream)
        {
            var xmlSer = new System.Xml.Serialization.XmlSerializer(typeof(RecoverableSerialForm));
            var ser = (RecoverableSerialForm)xmlSer.Deserialize(stream);
            return ser.Recover();
        }

        [XmlType(nameof(PkiCertificateSigningRequest))]
        public class RecoverableSerialForm
        {
            public RecoverableSerialForm()
            { }
            
            public RecoverableSerialForm(PkiCertificateSigningRequest csr)
            {
                _subject = csr.SubjectName;
                _keypair = csr._keyPair == null ? null : new PkiKeyPair.RecoverableSerialForm(csr._keyPair);
                _pubkey = new PkiKey.RecoverableSerialForm(csr.PublicKey);
                _hashalgor = csr.HashAlgorithm;
                _exts = csr.CertificateExtensions.Select(x =>
                    (x.Identifier.Id, x.IsCritical,
                            x.Value.ToAsn1Object().GetDerEncoded())).ToArray();
            }

            public int _ver = 1;
            public string _subject;
            public PkiKeyPair.RecoverableSerialForm _keypair;
            public PkiKey.RecoverableSerialForm _pubkey;
            public PkiHashAlgorithm _hashalgor;
            public (string id, bool crit, byte[] value)[] _exts;

            public PkiCertificateSigningRequest Recover()
            {
                var csr = new PkiCertificateSigningRequest(_subject, _keypair?.Recover(), _hashalgor);

                foreach (var e in _exts)
                {
                    csr.CertificateExtensions.Add(new PkiCertificateExtension
                    {
                        Identifier = new DerObjectIdentifier(e.id),
                        IsCritical = e.crit,
                        Value = Asn1Object.FromByteArray(e.value),
                    });
                }

                return csr;
            }
        }
    }
}
