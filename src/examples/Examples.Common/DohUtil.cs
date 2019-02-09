using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Makaretu.Dns;

namespace Examples.Common
{
    /// <summary>
    /// Utility class to resolve DNS queries using DNS-over-HTTP (DoH).
    /// </summary>
    public static class DohUtil
    {
        private static DohClient _Client;

        /// <summary>
        /// Defaults to "https://cloudflare-dns.com/dns-query"
        /// </summary>
        public static string DohServerUrl { get; set; }

        public static HttpClient HttpClient { get; set; }

        public static DohClient Client
        {
            get
            {
                if (_Client == null)
                {
                    lock (typeof(DohUtil))
                    {
                        if (_Client == null)
                        {
                            _Client = new DohClient();

                            if (HttpClient != null)
                                _Client.HttpClient = HttpClient;
                            if (DohServerUrl != null)
                                _Client.ServerUrl = DohServerUrl;
                        }
                    }
                }
                return _Client;
            }
        }

        public static async Task<IEnumerable<string>> LookupRawRecordsAsync(string type, string name)
        {
            if (!Enum.TryParse<Makaretu.Dns.DnsType>(type, out var dnsType))
                throw new Exception("invalid or unsupported DNS type");

            var response = await Client.QueryAsync(name, dnsType);
            return response.Answers.Select(x => x.ToString());
        }

        public static async Task<IEnumerable<string>> LookupTxtRecordsAsync(string name)
        {
            var response = await Client.QueryAsync(name, Makaretu.Dns.DnsType.TXT);
            return response.Answers
                    .OfType<Makaretu.Dns.TXTRecord>()
                    .SelectMany(x => x.Strings);
        }

        public static async Task<IEnumerable<IPAddress>> LookupIpv4RecordsAsync(string name)
        {
            var response = await Client.QueryAsync(name, Makaretu.Dns.DnsType.A);
            return response.Answers
                    .OfType<Makaretu.Dns.ARecord>()
                    .Select(x => x.Address);
        }

        public static async Task<IEnumerable<IPAddress>> LookupIpv6RecordsAsync(string name)
        {
            var response = await Client.QueryAsync(name, Makaretu.Dns.DnsType.AAAA);
            return response.Answers
                    .OfType<Makaretu.Dns.AAAARecord>()
                    .Select(x => x.Address);
        }

        public static async Task<IEnumerable<string>> LookupCnameRecordsAsync(string name)
        {
            var dns = new Makaretu.Dns.DnsClient();
            var response = await Client.QueryAsync(name, Makaretu.Dns.DnsType.CNAME);
            return response.Answers
                    .OfType<Makaretu.Dns.CNAMERecord>()
                    .Select(x => x.Target);
        }
    }
}