namespace ACMESharp.Authorizations
{
    public class Dns01ChallengeDetails
    {
        public const string ChallengeType = "dns-01";
        public const string DnsRecordNamePrefix = "_acme-challenge";
        public const string DnsRecordTypeDefault = "TXT";

        public string DnsRecordName { get; set; }

        public string DnsRecordType { get; set; }

        public string DnsRecordValue { get; set; }
    }
}