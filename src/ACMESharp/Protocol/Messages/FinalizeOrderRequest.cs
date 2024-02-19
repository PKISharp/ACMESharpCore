using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol.Resources;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class FinalizeOrderRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public string Csr { get; set; } = string.Empty;
    }
}