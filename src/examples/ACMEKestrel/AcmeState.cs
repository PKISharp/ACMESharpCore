using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ACMESharp.Authorizations;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;

namespace ACMEKestrel
{
    public class AcmeState
    {
        public static AcmeState Instance { get; } = new AcmeState();

        public const string PendingStatus = "pending";
        public const string ValidStatus = "valid";
        public const string InvalidStatus = "invalid";

        public const string Http01ChallengeType = "http-01";
        public const string Dns01ChallengeType = "dns-01";

        public string RootDir { get; set; }

        public string ServiceDirectoryFile { get; set; }

        public ServiceDirectory ServiceDirectory { get; set; }

        public string TermsOfServiceFile { get; set; }

        public string AccountFile { get; set; }

        public AccountDetails Account { get; set; }

        public string AccountKeyFile { get; set; }

        public AccountKey AccountKey { get; set; }

        public string OrderFile { get; set; }

        public OrderDetails Order { get; set; }

        public string AuthorizationsFile { get; set; }

        public Dictionary<string, Authorization> Authorizations { get; set; }

        public string CertificateKeysFile { get; set; }

        public string CertificateRequestFile { get; set; }

        public string CertificateChainFile { get; set; }

        public string CertificateFile { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public IDictionary<string, Http01ChallengeValidationDetails> Http01Responses { get; set; }
                = new Dictionary<string, Http01ChallengeValidationDetails>();
    }
}