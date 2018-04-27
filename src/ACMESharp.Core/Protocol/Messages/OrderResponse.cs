using System.ComponentModel.DataAnnotations;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
    /// </summary>
    public class OrderResponse
    {
        [Required]
        public string Status { get; set; }

        public string Expires { get; set; }

        [Required, MinLength(1)]
        public Identifier[] Identifiers { get; set; }

        public string NotBefore { get; set; }

        public string NotAfter { get; set; }

        public Problem Error { get; set; }

        [Required, MinLength(1)]
        public string[] Authorizations { get; set; }

        [Required]
        public string Finalize { get; set; }

        public string Certificate { get; set; }
    }

    public class Identifier
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public string Value { get; set; }
    }

    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.4
    /// </summary>
    public class Authorization
    {
        [Required]
        public Identifier Identifier { get; set; }

        [Required]
        public string Status { get; set; }

        public string Expires { get; set; }

        [Required]
        public Challenge[] Challenges { get; set; }

        public bool? Wildcard { get; set; }
    }

    public class Problem
    {

    }

    public class Challenge
    {
        public string Type { get; set; }
        public string Url { get; set; }
        public string Status { get; set; }
        public string Token { get; set; }
        public string Validated { get; set; }
    }

    public class Http01Challenge : Challenge
    { }
}