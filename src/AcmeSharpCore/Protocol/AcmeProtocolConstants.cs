namespace AcmeSharpCore.Protocol
{
    public class AcmeProtocolConstants
    {

        /// <summary>
        /// Identifier type indicator indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-5.3"
        /// >fully-qualified domain name (DNS)</see>.
        /// </summary>
        public const string DnsIdentifierType = "dns";

        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.5"
        /// >DNS</see>.
        /// </summary>
        public const string DnsChallengeType = "dns-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.2"
        /// >HTTP (non-SSL/TLS)</see>.
        /// </summary>
        public const string HttpChallengeType = "http-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.3"
        /// >TLS SNI</see>.
        /// </summary>
        public const string TlsSniChallengeType = "tls-sni-01";
        /// <summary>
        /// Identifier validation challenge type indicator for
        /// <see cref="https://tools.ietf.org/html/draft-ietf-acme-acme-01#section-7.4"
        /// >Proof of Possession of a Prior Key</see>.  Currently UNSUPPORTED.
        /// </summary>
        public const string PriorKeyChallengeType = "proofOfPossession-01";


        public const string DnsChallengeNamePrefix = "_acme-challenge.";

        public const string DnsChallengeRecordType = "TXT";

        public const string HttpChallengePathPrefix = ".well-known/acme-challenge/";
    }
}