using System.ComponentModel.DataAnnotations;
using ACMESharp.Crypto.JOSE;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class CreateAccountRequest
    {
        [JsonProperty("contact", Required = Required.Always)]
        [Required, MinLength(1)]
        public string[] Contact { get; set; }

        [JsonProperty("termsOfServiceAgreed", NullValueHandling=NullValueHandling.Ignore)]
        public bool? TermsOfServiceAgreed { get; set; }

        [JsonProperty("onlyReturnExisting", NullValueHandling=NullValueHandling.Ignore)]
        public bool? OnlyReturnExisting  { get; set; }

        [JsonProperty("externalAccountBinding", NullValueHandling=NullValueHandling.Ignore)]
        public JwsSignedPayload ExternalAccountBinding { get; set; }
    }
}