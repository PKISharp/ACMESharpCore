namespace ACMESharp.Protocol.Model
{
    public class AccountDetails
    {
        public Account Payload { get; set; }

        public string Kid { get; set; }

        public string TosLink { get; set; }
    }
}