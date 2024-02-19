using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ACMESharp.Crypto.JOSE;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.2
    /// </summary>
    public class DeactivateAuthorizationRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Status { get => "deactivated"; }
    }
}