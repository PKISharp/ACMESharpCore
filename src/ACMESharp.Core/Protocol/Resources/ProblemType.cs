namespace ACMESharp.Protocol.Resources
{
    /// <summary>
    /// Defines standard ACME errors.
    /// </summary>
    /// <remarks>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-6.6
    /// </remarks>
    public enum ProblemType
    {
        Unknown = 0,

        AccountDoesNotExist,
        BadCSR,
        BadNonce,
        BadRevocationReason,
        BadSignatureAlgorithm,
        Caa,
        Compound,
        Connection,
        Dns,
        ExternalAccountRequired,
        IncorrectResponse,
        InvalidContact,
        Malformed,
        RateLimited,
        RejectedIdentifier,
        ServerInternal,
        Tls,
        Unauthorized,
        UnsupportedContact,
        UnsupportedIdentifier,
        UserActionRequired,
    }
}