using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Resources
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.4
    /// </summary>
    public class Authorization
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public Identifier Identifier { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public string Status { get; set; } = string.Empty;

        public string Expires { get; set; }

        [Required]
        public Challenge[] Challenges { get; set; }

        public bool? Wildcard { get; set; }
    }
}