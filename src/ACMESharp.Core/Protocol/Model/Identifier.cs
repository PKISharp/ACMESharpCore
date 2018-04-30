using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Model
{
    public class Identifier
    {
        [JsonProperty("type", Required = Required.Always)]
        [Required]
        public string Type { get; set; }

        [JsonProperty("value", Required = Required.Always)]
        [Required]
        public string Value { get; set; }
    }
}