using System.ComponentModel.DataAnnotations;
using ACMESharp.Crypto.JOSE;
using Newtonsoft.Json;

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
        [JsonProperty("contact")]
        public string[] Contact { get; set; }

        [JsonProperty("termsOfServiceAgreed", NullValueHandling=NullValueHandling.Ignore)]
        public bool? TermsOfServiceAgreed { get; set; }

        [JsonProperty("externalAccountBinding", NullValueHandling=NullValueHandling.Ignore)]
        public object ExternalAccountBinding { get; set; }
        //public JwsSignedPayload ExternalAccountBinding { get; set; }

        [JsonProperty("status", NullValueHandling=NullValueHandling.Ignore)]
        public string Status { get; set; }
    }
}