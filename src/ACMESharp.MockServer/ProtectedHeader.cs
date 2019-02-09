using System.Collections.Generic;

namespace ACMESharp.MockServer
{
    public class ProtectedHeader
    {
        public string Alg { get; set; }

        public string Kid { get; set; }

        public string Url { get; set; }

        public string Nonce { get; set; }

        public Dictionary<string, string> Jwk { get; set; }
    }
}