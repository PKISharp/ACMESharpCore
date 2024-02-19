using System.ComponentModel.DataAnnotations;

namespace ACMESharp.Protocol.Resources
{
    public class Challenge
    {
        public string Type { get; set; }

        public string Url { get; set; }

        public string Status { get; set; }

        /// <summary>
        /// The time at which the server validated this challenge,
        /// encoded in the format specified in RFC 3339 [RFC3339].
        /// This field is REQUIRED if the "status" field is "valid".
        /// </summary>
        public string Validated { get; set; }

        /// <summary>
        /// Details of successful validation.
        /// </summary>
        /// <remarks>
        /// TODO:  This does not appear to be documented in the latest ACMEv2 draft
        /// but experimentation shows a record such as this:
        /// <code>
        ///    "validationRecord": [
        ///        {
        ///          "hostname": "foo.example.com"
        ///        }
        ///    ]
        /// </code>
        /// There also does not appear to be any indication of the "validated
        /// </remarks>
        public object[] ValidationRecord { get; set; }

        /// <summary>
        /// Error that occurred while the server was validating the challenge,
        /// if any, structured as a problem document [RFC7807]. Multiple
        /// errors can be indicated by using subproblems Section 6.6.1.
        /// </summary>
        public Problem Error { get; set; }

        public string Token { get; set; }
    }
}