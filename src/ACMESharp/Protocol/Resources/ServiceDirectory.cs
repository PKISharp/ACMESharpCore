using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Resources
{
    public class ServiceDirectory
    {
        [JsonExtensionData]
        private IDictionary<string, object> _extra;

        public string Directory { get; set; } = "directory";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NewNonce { get; set; } //! = "acme/new-nonce";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NewAccount { get; set; } //! = "acme/new-acct";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NewOrder { get; set; } //! = "acme/new-order";


        /// <summary>
        /// This is an optional resource that an ACME CA may support
        /// if it supports Pre-Authorizations.
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4.1
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NewAuthz { get; set; } //! = "acme/new-authz";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RevokeCert { get; set; } //! = "acme/revoke-cert";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? KeyChange { get; set; } //! = "acme/key-change";

        public DirectoryMeta Meta { get; set; }

        public IEnumerable<string>? GetExtraNames() => _extra?.Keys;

        public object? GetExtra(string name) => _extra?[name];

        public void SetExtra(string name, object value)
        {
            if (_extra == null)
                _extra = new Dictionary<string, object>();
            _extra[name] = value;
        }
    }

    public class DirectoryMeta
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TermsOfService { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Website { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? CaaIdentities { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ExternalAccountRequired { get; set; }
    }
}
