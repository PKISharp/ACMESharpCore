using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class CreateAccountRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Required, MinLength(1)]
        public IEnumerable<string> Contact { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TermsOfServiceAgreed { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? OnlyReturnExisting  { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? ExternalAccountBinding { get; set; }
        //public JwsSignedPayload ExternalAccountBinding { get; set; }
    }
}