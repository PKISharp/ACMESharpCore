using System;
using ACMESharp.Protocol.Model;

namespace ACMESharp
{
    public class AcmeOrder
    {
        public string OrderUrl { get; set; }

        public string Status { get; set; }

        public DateTime Expires { get; set; }

        public string[] DnsIdentifiers { get; set; }

        public AcmeAuthorization[] Authorizations { get; set; }

        public string FinalizeUrl { get; set; }
    }
}