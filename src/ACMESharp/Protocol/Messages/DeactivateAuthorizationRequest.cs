using System.ComponentModel.DataAnnotations;
using ACMESharp.Crypto.JOSE;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.2
    /// </summary>
    public class DeactivateAuthorizationRequest
    {
        [JsonProperty("status", NullValueHandling=NullValueHandling.Ignore)]
        public string Status { get => "deactivated"; }
    }
}