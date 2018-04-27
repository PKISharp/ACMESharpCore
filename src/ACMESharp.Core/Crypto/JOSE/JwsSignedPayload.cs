using Newtonsoft.Json;

namespace ACMESharp.Crypto.JOSE
{
    public class JwsSignedPayload
    {
        [JsonProperty("header", NullValueHandling = NullValueHandling.Ignore)]
        public object Header
        { get; set; }

        [JsonProperty("protected", NullValueHandling = NullValueHandling.Ignore)]
        public string Protected
        { get; set; }

        [JsonProperty("payload", Required = Required.Always)]
        public string Payload
        { get; set; }

        [JsonProperty("signature", Required = Required.Always)]
        public string Signature
        { get; set; }
    }
}