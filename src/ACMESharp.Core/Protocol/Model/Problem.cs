using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Model
{
    public class Problem
    {
        public const string StandardProblemTypeNamespace = "urn:ietf:params:acme:error:";

        [JsonProperty("type", Required = Required.Always)]
        [Required]
        public string Type { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("status")]
        public int? Status { get; set; }
    }
}
