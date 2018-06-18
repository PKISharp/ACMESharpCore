using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace ACMESharp.Crypto.PKI
{
    public class PkiCertificateExtension
    {
        internal PkiCertificateExtension()
        { }

        internal DerObjectIdentifier Identifier { get; set; }

        internal X509Extension Value { get; set; }

        public static PkiCertificateExtension CreateDnsSubjectAlternativeNames(IEnumerable<string> dnsNames)
        {
            var gnames = new List<GeneralName>(
                    dnsNames.Select(x => new GeneralName(GeneralName.DnsName, x)));

            var altNames = new GeneralNames(gnames.ToArray());

            return new PkiCertificateExtension
            {
                Identifier = X509Extensions.SubjectAlternativeName,
                Value = new X509Extension(false, new DerOctetString(altNames)),
            };
        }
    }
}