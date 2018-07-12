using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;

namespace ACMECLI
{
    public static class DnsUtil
    {
        private static LookupClient _Client;

        public static string[] DnsServers { get; set; }

        public static LookupClient Client
        {
            get
            {
                if (_Client == null)
                {
                    lock (typeof(DnsUtil))
                    {
                        if (_Client == null)
                        {
                            if (DnsServers?.Length > 0)
                            {
                                var nameServers = DnsServers.SelectMany(x => Dns.GetHostAddresses(x)).ToArray();
                                _Client = new DnsClient.LookupClient(nameServers);
                            }
                            else
                            {
                                _Client = new DnsClient.LookupClient();
                            }
                        }
                    }
                }
                return _Client;
            }
        }
        
        public static async Task<IEnumerable<string>> LookupRecordAsync(string type, string name)
        {
            var dnsType = (DnsClient.QueryType)Enum.Parse(typeof(DnsClient.QueryType), type);
            var dnsResp = await Client.QueryAsync(name, dnsType);

            if (dnsResp.HasError)
            {
                if ("Non-Existent Domain".Equals(dnsResp.ErrorMessage,
                        StringComparison.OrdinalIgnoreCase))
                    return null;
                throw new Exception("DNS lookup error:  " + dnsResp.ErrorMessage);
            }

            return dnsResp.AllRecords.SelectMany(x => x.ValueAsStrings());
        }

        public static IEnumerable<string> ValueAsStrings(this DnsResourceRecord drr)
        {
            switch (drr)
            {
                case TxtRecord txt:
                    return txt.Text;
                default:
                    return new[] { drr.ToString() };
            }
        }
    }
}