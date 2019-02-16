using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

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

        // Possible reasons specified here
        // https://tools.ietf.org/html/rfc5280#section-5.3.1
        // Not sure where best to create an enum and how to handle the (optional) 
        // serialization of this property, so leaving it out for now
        //[JsonProperty("reason")]
        //public int Reason { get; set; }
    }
}