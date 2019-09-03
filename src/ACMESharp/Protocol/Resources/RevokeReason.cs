namespace ACMESharp.Protocol.Resources
{
    /// <summary>
    /// Reasons for revocation
    /// https://tools.ietf.org/html/rfc5280#section-5.3.1
    /// </summary>
    public enum RevokeReason
    {
        Unspecified = 0,
        KeyCompromise = 1,
        CaCompromise = 2,
        AffiliationChanged = 3,
        Superseded = 4,
        CessationOfOperation = 5,
        CertificateHold = 6,
        /*Value 7 is not used*/
        RemoveFromCrl = 8,
        PrivilegeWithdrawn = 9,
        AaCompromise = 10
    }
}
