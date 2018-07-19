using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEForms.Storage
{
    public class DbAccount
    {
        public static readonly IEnumerable<KeyValuePair<string, string>> WellKnownAcmeServers =
            new (string key, string label)[]
            {
                ("https://acme-staging-v02.api.letsencrypt.org/", "Let's Encrypt v2 STAGE"),
                ("https://acme-v02.api.letsencrypt.org/", "Let's Encrypt v2"),
                (string.Empty, "(CUSTOM)"),
            }.Select(x => new KeyValuePair<string, string>(x.key, x.label));

        public string AcmeServerEndpoint { get; set; }

        public int Id { get; set; }

        public string JwsSigner { get; set; }

        public AccountDetails Details { get; set; }
    }
}
