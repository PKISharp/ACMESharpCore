namespace ACMESharp.Authorizations
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-tls-alpn-05
    /// </summary>
    public class TlsAlpn01ChallengeValidationDetails : IChallengeValidationDetails
    {
        public const string TlsAlpn01ChallengeType = "tls-alpn-01";
        public const string AlpnExtensionName = "acme-tls/1";
        public const string AcmeIdentifierExtension = "acmeIdentifier";

        public string ChallengeType => TlsAlpn01ChallengeType;

        public string TokenValue { get; set; }
    }
}