using ACMESharp.Protocol.Resources;

namespace ACMESharp.MockServer.Storage
{
    public class DbAuthorization
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Url { get; set; }

        public Authorization Payload { get; set; }
    }
}