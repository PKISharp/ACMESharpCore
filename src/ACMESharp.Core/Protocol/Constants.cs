namespace ACMESharp.Protocol
{
    public class Constants
    {
        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.1
        /// </summary>
        public const string UserAgentHeaderValue = "ACMESharp/1.9.0";

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.1
        /// </summary>
        public const string AcceptLanguageHeaderValue = "en-us";

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.2
        /// </summary>
        public const string ContentTypeHeaderValue = "application/jose+json";
        public const string ContentTypeHeaderName = "Content-Type";

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.6
        /// </summary>
        public const string ErrorTypeNamespacePrefix = "urn:ietf:params:acme:error:";

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.2
        /// </summary>
        public const string ReplayNonceHeaderName = "Replay-Nonce";
    }
}