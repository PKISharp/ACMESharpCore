using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    public class KeyChangeRequest
    {
        [JsonProperty("account", Required = Required.Always)]
        [Required]
        public string Account { get; set; }

        [JsonProperty("newKey", Required = Required.Always)]
        [Required]
        public object NewKey { get; set; }
    }
}