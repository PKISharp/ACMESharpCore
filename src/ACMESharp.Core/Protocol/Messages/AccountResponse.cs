using System;
using System.Collections.Generic;

namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.2
    /// </summary>
    public class AccountResponse
    {
        public string Status { get; set; }

        public string[] Contact { get; set; }
        
        public bool? TermsOfServiceAgreed { get; set; }

        public string Orders { get; set; }

        public AccountStatus GeStatus()
        {
            switch (Status)
            {
                case "valid": return AccountStatus.Valid;
                case "deactivated": return AccountStatus.Deactivated;
                case "revoked": return AccountStatus.Revoked;
                default: return AccountStatus.Unknown;
            }
        }
    }

    public enum AccountStatus
    {
        Unknown = 0,
        Valid = 1,
        Deactivated = 2,
        Revoked = 3,
    }
}