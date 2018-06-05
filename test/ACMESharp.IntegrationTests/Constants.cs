namespace ACMESharp.IntegrationTests
{
    public class Constants
    {
        /// https://letsencrypt.org/docs/staging-environment/
        /// https://letsencrypt.status.io/

        public const string LetsEncryptStagingEndpoint = "https://acme-staging.api.letsencrypt.org/";
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";
        public const string LetsEncryptEndpoint = "https://acme-v01.api.letsencrypt.org/";
        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";        
    }
}