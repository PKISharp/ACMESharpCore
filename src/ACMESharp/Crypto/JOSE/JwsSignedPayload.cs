using System.Text.Json.Serialization;

namespace ACMESharp.Crypto.JOSE
{
    public class JwsSignedPayload
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object Header { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Protected{ get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Payload { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public string Signature { get; set; }
    }
}