namespace AcmeSharpCore.Protocol
{
    public static class AcmeHttpConstants
    {
        // HTTP Methods

        public const string HeadMethod = "HEAD";
        public const string GetMethod = "GET";
        public const string PostMethod = "POST";

        // MIME Content Types

        public const string JsonContentType = "application/json";

        // HTTP Header Names

        public const string ReplaceNonceHeader = "Replace-nonce";
        public const string LocationHeader = "Location";
        public const string LinkHeader = "Link";
        public const string RetryAfterHeader = "Retry-After";

        // HTTP Header Values

        public const string UserAgentFormat = "AcmeSharpCore/{0} (ACME 1.0)";

        /// <summary>
        /// The relation name for the "Terms of Service" related link header.
        /// </summary>
        /// <remarks>
        /// Link headers can be returned as part of a registration:
        ///   HTTP/1.1 201 Created
        ///   Content-Type: application/json
        ///   Location: https://example.com/acme/reg/asdf
        ///   Link: <https://example.com/acme/new-authz>;rel="next"
        ///   Link: <https://example.com/acme/recover-reg>;rel="recover"
        ///   Link: <https://example.com/acme/terms>;rel="terms-of-service"
        ///
        /// The "terms-of-service" URI should be included in the "agreement" field
        /// in a subsequent registration update
        /// </remarks>
        public const string TermsOfServiceRelLink = "terms-of-service";
    }
}