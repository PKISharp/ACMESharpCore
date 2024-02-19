using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Resources
{
    public class Problem
    {
        public const string StandardProblemTypeNamespace = "urn:ietf:params:acme:error:";

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required]
        public string Type { get; set; }

        public string Detail { get; set; }

        public int? Status { get; set; }
    }
}
