using System;
using System.Collections.Generic;
using ACMESharp.Protocol.Resources;

namespace ACMESharp
{
    public class AcmeOrder
    {
        public string OrderUrl { get; set; }

        public string Status { get; set; }

        public DateTime Expires { get; set; }

        public string[] DnsIdentifiers { get; set; }

        public IEnumerable<AcmeAuthorization> Authorizations { get; set; }

        public string FinalizeUrl { get; set; }

        public string CertificateUrl { get; set; }
    }
}