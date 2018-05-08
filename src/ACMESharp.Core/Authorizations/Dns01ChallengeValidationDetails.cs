namespace ACMESharp.Authorizations
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8.4
    /// </summary>
    public class Dns01ChallengeValidationDetails : IChallengeValidationDetails
    {
        public const string Dns01ChallengeType = "dns-01";
        public const string DnsRecordNamePrefix = "_acme-challenge";
        public const string DnsRecordTypeDefault = "TXT";

        public string ChallengeType => Dns01ChallengeType;

        public string DnsRecordName { get; set; }

        public string DnsRecordType { get; set; }

        public string DnsRecordValue { get; set; }
    }
}