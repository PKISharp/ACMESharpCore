using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace PKISharp.SimplePKI
{
    public class PkiCertificateExtension : IComparable
    {
        internal PkiCertificateExtension()
        { }

        internal DerObjectIdentifier Identifier { get; set; }

        internal bool IsCritical { get; set; }

        internal Asn1Encodable Value { get; set; }
        // internal X509Extension Value { get; set; }

        public int CompareTo(object obj)
        {
            var that = obj as PkiCertificateExtension;
            if (that == null)
                return -1;

            var thisVal = this.Identifier.ToString()
                    + this.IsCritical
                    + Convert.ToBase64String(this.Value.GetDerEncoded());
            var thatVal = that.Identifier.ToString()
                    + that.IsCritical
                    + Convert.ToBase64String(that.Value.GetDerEncoded());
            return thisVal.CompareTo(thatVal);
        }

        public static PkiCertificateExtension CreateDnsSubjectAlternativeNames(IEnumerable<string> dnsNames)
        {
            // Based on:
            //    https://boredwookie.net/blog/bouncy-castle-add-a-subject-alternative-name-when-creating-a-cer

            var gnames = new List<GeneralName>(
                    dnsNames.Select(x => new GeneralName(GeneralName.DnsName, x)));

            var altNames = new GeneralNames(gnames.ToArray());

            return new PkiCertificateExtension
            {
                Identifier = X509Extensions.SubjectAlternativeName,
                IsCritical = false,
                Value = altNames,
                //Value = new X509Extension(false, new DerOctetString(altNames)),
            };
        }
    }
}