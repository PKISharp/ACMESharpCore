using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using ACMESharp.Protocol.Resources;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-18#section-7.6
    /// </summary>
    public class RevokeCertificateRequest
    {
        [JsonProperty("certificate", Required = Required.Always)]
        [Required]
        public string Certificate { get; set; }

        [JsonProperty("reason")]
        public RevokeReason Reason { get; set; } = RevokeReason.Unspecified;
    }
}