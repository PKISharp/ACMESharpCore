using System.ComponentModel.DataAnnotations;
using ACMESharp.Protocol.Model;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class OrderResponse
    {
        [JsonProperty("status")]
        [Required]
        public string Status { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        [JsonProperty("notBefore")]
        public string NotBefore { get; set; }

        [JsonProperty("notAfter")]
        public string NotAfter { get; set; }

        [JsonProperty("identifiers")]
        [Required, MinLength(1)]
        public Identifier[] Identifiers { get; set; }

        [JsonProperty("authorizations")]
        [Required, MinLength(1)]
        public string[] Authorizations { get; set; }

        [JsonProperty("finalize")]
        [Required]
        public string Finalize { get; set; }

        [JsonProperty("certificate", NullValueHandling = NullValueHandling.Ignore)]
        public string Certificate { get; set; }
        
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public Problem Error { get; set; }
    }
}