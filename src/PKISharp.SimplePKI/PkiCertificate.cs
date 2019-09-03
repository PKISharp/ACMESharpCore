using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using BclCertificate = System.Security.Cryptography.X509Certificates.X509Certificate2;

namespace PKISharp.SimplePKI
{
    public class PkiCertificate
    {
        internal PkiCertificate()
        { }

        public string SubjectName => NativeCertificate.SubjectDN.ToString();

        public IEnumerable<string> SubjectAlternativeNames =>
                NativeCertificate.GetSubjectAlternativeNames()?.Cast<ArrayList>()
                        .SelectMany(x => x.Cast<object>().Where(y => y is string)
                                .Select(y => (string)y));

        internal X509Certificate NativeCertificate { get; set; }

        public BclCertificate ToBclCertificate()
        {
            return new BclCertificate(NativeCertificate.GetEncoded());
        }

        public byte[] Export(PkiEncodingFormat format)
        {
            switch (format)
            {
                case PkiEncodingFormat.Pem:
                    using (var sw = new StringWriter())
                    {
                        var pemWriter = new PemWriter(sw);
                        pemWriter.WriteObject(NativeCertificate);
                        return Encoding.UTF8.GetBytes(sw.GetStringBuilder().ToString());
                    }

                case PkiEncodingFormat.Der:
                    return NativeCertificate.GetEncoded();
                
                default:
                    throw new NotSupportedException();
            }
        }

        public byte[] Export(PkiArchiveFormat format, PkiKey privateKey = null,
            IEnumerable<PkiCertificate> chain = null,
            char[] password = null)
        {
            // Based on:
            //    https://stackoverflow.com/a/44798441/5428506

            switch (format)
            {
                case PkiArchiveFormat.Pem:
                    using (var buff = new MemoryStream())
                    {
                        byte[] bytes = privateKey?.Export(PkiEncodingFormat.Pem, password);
                        if (bytes != null)
                            buff.Write(bytes, 0, bytes.Length);
                        bytes = Export(PkiEncodingFormat.Pem);
                        buff.Write(bytes, 0, bytes.Length);
                        if (chain != null)
                        {
                            foreach (var c in chain)
                            {
                                bytes = c.Export(PkiEncodingFormat.Pem);
                                buff.Write(bytes, 0, bytes.Length);
                            }
                        }
                        return buff.ToArray();
                    }

                case PkiArchiveFormat.Pkcs12:
                    var alias = AliasOf(this);
                    var store = new Pkcs12StoreBuilder().Build();
                    if (privateKey != null)
                        store.SetKeyEntry(alias, new AsymmetricKeyEntry(privateKey.NativeKey),
                                new[] { new X509CertificateEntry(NativeCertificate) });
                    else
                        store.SetCertificateEntry(alias, new X509CertificateEntry(NativeCertificate));

                    if (chain != null)
                    {
                        foreach (var c in chain)
                        {
                            store.SetCertificateEntry(AliasOf(c),
                                    new X509CertificateEntry(c.NativeCertificate));
                        }
                    }
                    using (var buff = new MemoryStream())
                    {
                        store.Save(buff, password ?? new char[0], new SecureRandom());
                        return buff.ToArray();
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        internal string AliasOf(PkiCertificate cert)
        {
            var x509Name = new X509Name(cert.SubjectName);
            return (x509Name.GetValueList(X509Name.CN)?[0] ?? cert.SubjectName) as string;
        }

        public void Save(Stream stream)
        {
            var xmlser = new XmlSerializer(typeof(RecoverableSerialForm));
            var ser = new RecoverableSerialForm(this);
            xmlser.Serialize(stream, ser);
        }

        public static PkiCertificate Load(Stream stream)
        {
            var xmlser = new XmlSerializer(typeof(RecoverableSerialForm));
            var ser = (RecoverableSerialForm)xmlser.Deserialize(stream);
            return ser.Recover();
        }

        [XmlType(nameof(PkiCertificate))]
        public class RecoverableSerialForm
        {
            public RecoverableSerialForm()
            { }

            public RecoverableSerialForm(PkiCertificate cert)
            {
                _certificate = cert.Export(PkiEncodingFormat.Der);
                _sn = cert.SubjectName;
                _san = cert.SubjectAlternativeNames?.ToArray();
            }

            public int _ver = 1;
            public byte[] _certificate;
            public string _sn;
            public string[] _san;

            public PkiCertificate Recover()
            {
                return new PkiCertificate
                {
                    NativeCertificate = new X509CertificateParser().ReadCertificate(_certificate),
                }; 
            }
        }
    }
}