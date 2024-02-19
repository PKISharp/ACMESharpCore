using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ACMESharp.Crypto.JOSE;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class DeactivateAccountRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Status { get => "deactivated"; }
    }
}