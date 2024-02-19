using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Resources
{
    public class Identifier
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public string Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public string Value { get; set; }
    }
}