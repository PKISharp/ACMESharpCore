using ACMESharp.Protocol.Resources;

namespace ACMESharp.MockServer.Storage
{
    public class DbChallenge
    {
        public int Id { get; set; }

        public int AuthorizationId { get; set; }

        public Challenge Payload { get; set; }
    }
}