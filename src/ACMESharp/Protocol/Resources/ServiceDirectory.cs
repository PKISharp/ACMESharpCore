using System.Collections.Generic;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Resources
{
    public class ServiceDirectory
    {
        [JsonExtensionData]
        private IDictionary<string, object> _extra;

        public string Directory { get; set; } = "/directory";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NewNonce { get; set; } //! = "/acme/new-nonce";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NewAccount { get; set; } //! = "/acme/new-acct";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NewOrder { get; set; } //! = "/acme/new-order";


        /// <summary>
        /// This is an optional resource that an ACME CA may support
        /// if it supports Pre-Authorizations.
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4.1
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string NewAuthz { get; set; } //! = "/acme/new-authz";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RevokeCert { get; set; } //! = "/acme/revoke-cert";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string KeyChange { get; set; } //! = "/acme/key-change";

        public DirectoryMeta Meta { get; set; }

        public IEnumerable<string> GetExtraNames() => _extra?.Keys;

        public object GetExtra(string name) => _extra?[name];

        public void SetExtra(string name, object value)
        {
            if (_extra == null)
                _extra = new Dictionary<string, object>();
            _extra[name] = value;
        }
    }

    public class DirectoryMeta
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TermsOfService { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Website { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[] CaaIdentities { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExternalAccountRequired { get; set; }
    }
}