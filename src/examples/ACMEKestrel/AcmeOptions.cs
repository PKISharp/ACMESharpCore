using System;
using System.Collections.Generic;
using ACMESharp.Authorizations;
using ACMESharp.Protocol.Resources;

namespace ACMEKestrel
{
    public class AcmeOptions
    {
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";

        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";

        public const int DefaultRsaKeySize = 2048;
        public const int DefaultEcKeySize = 256;

        public string AcmeRootDir { get; set; } = "_acmesharp";

        public string CaUrl { get; set; } = LetsEncryptV2Endpoint;

        public string AccountKeyAlgor { get; set; } = "ec";

        public int? AccountKeySize { get; set; }

        public IEnumerable<string> AccountContactEmails { get; set; }

        public bool AcceptTermsOfService { get; set; }

        public IEnumerable<string> DnsNames { get; set; }
       
        public string ChallengeType { get; } = AcmeState.Http01ChallengeType;

        public Func<IServiceProvider, IChallengeValidationDetails, bool> ChallengeHandler { get; set; }
                = AcmeHttp01ChallengeHandler.AddChallengeHandling;

        public bool TestChallenges { get; }

        public string CertificateKeyAlgor { get; set; } = "ec";

        public int? CertificateKeySize { get; set; }    

        public int WaitForAuthorizations { get; set; } = 60;

        public int WaitForCertificate { get; set; } = 60; 
    }
}