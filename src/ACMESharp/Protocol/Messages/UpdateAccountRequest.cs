using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class UpdateAccountRequest
    {
        /// <summary>
        /// The list of contact URLs.  Although a request to create a brand new account
        /// requires this value, when used in a request to lookup an existing account
        /// this property can be omitted.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string>? Contact { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TermsOfServiceAgreed { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ExternalAccountBinding { get; set; }
        //public JwsSignedPayload ExternalAccountBinding { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Status { get; set; }
    }
}