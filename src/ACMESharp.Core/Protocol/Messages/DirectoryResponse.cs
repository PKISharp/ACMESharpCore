namespace ACMESharp.Protocol.Messages
{
    public class DirectoryResponse
    {
        public string Directory { get; set; } = "/directory";

        public string NewNonce { get; set; } //! = "/acme/new-nonce";

        public string NewAccount { get; set; } //! = "/acme/new-acct";

        public string NewOrder { get; set; } //! = "/acme/new-order";


        /// <summary>
        /// This is an optional resource that an ACME CA may support
        /// if it supports Pre-Authorizations.
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4.1
        /// </summary>
        public string NewAuthz { get; set; } //! = "/acme/new-authz";
        public string RevokeCert { get; set; } //! = "/acme/revoke-cert";

        public string KeyChange { get; set; } //! = "/acme/key-change";

        public DirectoryMeta Meta { get; set; }
    }

    public class DirectoryMeta
    {
        public string TermsOfService { get; set; }

        public string Website { get; set; }

        public string[] CaaIdentities { get; set; }

        public string ExternalAccountRequired { get; set; }
    }
}