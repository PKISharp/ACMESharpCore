using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Resources
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.4
    /// </summary>
    public class Authorization
    {
        [JsonProperty("identifier", Required = Required.Always)]
        [Required]
        public Identifier Identifier { get; set; }

        [JsonProperty("status", Required = Required.Always)]
        [Required]
        public string Status { get; set; }

        [JsonProperty("expires")]
        public string Expires { get; set; }

        [JsonProperty("challenges")]
        [Required]
        public Challenge[] Challenges { get; set; }

        [JsonProperty("wildcard")]
        public bool? Wildcard { get; set; }
    }
}