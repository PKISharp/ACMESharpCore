namespace ACMESharp
{
    /// <summary>
    /// Represents the details of a registered ACME Account with a specific ACME CA.
    /// </summary>
    public class AcmeAccount
    {
        public string[] Contacts { get; set; }

        public object PublicKey { get; set; }

        /// <summary>
        /// This is the Key Identifier used in most messages sent to the ACME CA after
        /// the initial Account registration.  It references the public key that was
        /// registered with the Account in the JWS-signed Account create message.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.2
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
        /// <para>
        /// The KID is returned as a <c>Location</c> header in the Account
        /// creation response.
        /// </para>
        /// </remarks>
        /// <returns></returns>
        public string Kid { get; set; }

        public string TosLink { get; set; }

        /// <summary>
        /// CA-assigned unique identifier for the Account.
        /// </summary>
        /// <returns></returns>
        public string Id { get; set; }
    }
}