using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace ACMESharp.Protocol.Model
{
    public class Challenge
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// The time at which the server validated this challenge,
        /// encoded in the format specified in RFC 3339 [RFC3339].
        /// This field is REQUIRED if the "status" field is "valid".
        /// </summary>
        [JsonProperty("validate")]
        public string Validated { get; set; }

        /// <summary>
        /// Error that occurred while the server was validating the challenge,
        /// if any, structured as a problem document [RFC7807]. Multiple
        /// errors can be indicated by using subproblems Section 6.6.1.
        /// </summary>
        [JsonProperty("error")]
        public object Error { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}