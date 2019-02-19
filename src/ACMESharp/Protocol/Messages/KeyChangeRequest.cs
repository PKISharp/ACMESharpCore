using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// Based on:
    ///   https://tools.ietf.org/html/draft-ietf-acme-acme-18#section-7.3.5
    /// </summary>
    public class KeyChangeRequest
    {
        [JsonProperty("account", Required = Required.Always)]
        [Required]
        public string Account { get; set; }

        [JsonProperty("oldKey", Required = Required.Always)]
        [Required]
        public object OldKey { get; set; }
    }
}