using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class CreateAccountResponse
    {
        [JsonProperty("contact", Required = Required.Always)]
        [Required, MinLength(1)]
        public string[] Contact { get; set; }

        public object Key { get; set; }

        public string Id { get; set; }

        public string Status { get; set; }


        // TODO: are these standard or specific to LE?
        public string InitialIp { get; set; }
        public string CreatedAt { get; set; }
    }
}