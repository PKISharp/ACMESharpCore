using System.Collections.Generic;
using ACMESharp.Protocol.Resources;

namespace ACMESharp
{
    /// <summary>
    /// Represents the details of a registered ACME Account with a specific ACME CA.
    /// </summary>
    public class AcmeAccount(Account account, string kid, string tosLink) : AcmeAccount.IAccountDetails
    {

        Account IAccountDetails.AccountDetails => account;

        public IEnumerable<string> Contacts => account?.Contact;

        public object PublicKey => account?.Key;

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
        public string Kid { get; } = kid;

        public string TosLink { get; } = tosLink;

        /// <summary>
        /// CA-assigned unique identifier for the Account.
        /// </summary>
        /// <returns></returns>
        public string Id => account?.Id;

        public AccountStatus Status { get; }

        public AccountStatus GeStatus()
        {
            return (account?.Status) switch
            {
                "valid" => AccountStatus.Valid,
                "deactivated" => AccountStatus.Deactivated,
                "revoked" => AccountStatus.Revoked,
                _ => AccountStatus.Unknown,
            };
        }

        public enum AccountStatus
        {
            Unknown = 0,
            Valid = 1,
            Deactivated = 2,
            Revoked = 3,
        }

        public interface IAccountDetails
        {
            Account AccountDetails { get; }
        }
    }
}