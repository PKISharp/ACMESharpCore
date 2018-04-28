using System.ComponentModel.DataAnnotations;
using ACMESharp.Crypto.JOSE;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class DeactivateAccountRequest
    {
        [JsonProperty("status", NullValueHandling=NullValueHandling.Ignore)]
        public string Status { get => "deactivated"; }
    }
}