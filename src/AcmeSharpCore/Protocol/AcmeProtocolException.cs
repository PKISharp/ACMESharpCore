using System;
using System.Runtime.Serialization;

namespace AcmeSharpCore.Protocol
{
    public class AcmeProtocolException : AcmeException
    {
        public AcmeProtocolException(string message, AcmeHttpResponse response = null,
                Exception innerException = null) : base(message, innerException)
        {
            Response = response;
        }

        protected AcmeProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public AcmeHttpResponse Response
        { get; private set; }

        public override string Message
        {
            get
            {
                if (Response != null)
                    return base.Message + "\n +Response from server:\n\t+ Code: "
                            + Response.StatusCode.ToString() + "\n\t+ Content: "
                            + Response.ContentAsString;
                else
                    return base.Message + "\n +No response from server";
            }
        }

    }
}