using ACMESharp.Protocol.Resources;

namespace ACMESharp.Protocol
{
    /// <summary>
    /// An aggregation of Account details including resource payload and ancillary,
    /// associated data.
    /// </summary>
    /// <remarks>
    /// This represents a superset of details that are included in responses
    /// to several ACME operations regarding an ACME Account, such as Account
    /// registration, update, key rotation and deactivation.
    /// </remarks>
    public class AccountDetails
    {
        public Account Payload { get; set; }

        public string Kid { get; set; }

        public string TosLink { get; set; }
    }
}