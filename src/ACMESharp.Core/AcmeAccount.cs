using System.Collections.Generic;
using ACMESharp.Protocol.Model;

namespace ACMESharp
{
    /// <summary>
    /// Represents the details of a registered ACME Account with a specific ACME CA.
    /// </summary>
    public class AcmeAccount : AcmeAccount.IAccountDetails
    {
        private Account _account;

        public AcmeAccount(Account account, string kid, string tosLink)
        {
            _account = account;

            Kid = kid;
            TosLink = tosLink;
        }

        Account IAccountDetails.AccountDetails => _account;

        public IEnumerable<string> Contacts => _account?.Contact;

        public object PublicKey => _account?.Key;

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
        public string Kid { get; }

        public string TosLink { get; }

        /// <summary>
        /// CA-assigned unique identifier for the Account.
        /// </summary>
        /// <returns></returns>
        public string Id => _account?.Id;

        public AccountStatus Status { get; }

        public AccountStatus GeStatus()
        {
            switch (_account?.Status)
            {
                case "valid": return AccountStatus.Valid;
                case "deactivated": return AccountStatus.Deactivated;
                case "revoked": return AccountStatus.Revoked;
                default: return AccountStatus.Unknown;
            }
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