using ACMESharp.Protocol.Model;

namespace ACMESharp
{
    public class AcmeAuthorization
    {
        public string DetailsUrl { get; set; }

        public string FetchError { get; set; }

        public Authorization Details { get; set; }
    }
}