using System;
using System.Runtime.Serialization;
using ACMESharp.Protocol.Resources;

namespace ACMESharp.Protocol
{
    public class AcmeProtocolException : Exception
    {
        private Problem _problem;

        public AcmeProtocolException(Problem problem = null)
        {
            Init(problem);
        }

        public AcmeProtocolException(string message, Problem problem = null)
            : base(message)
        {
            Init(problem);
        }

        public AcmeProtocolException(string message, Exception innerException, Problem problem = null)
            : base(message, innerException)
        {
            Init(problem);
        }

        protected AcmeProtocolException(SerializationInfo info, StreamingContext context, Problem problem = null)
            : base(info, context)
        {
            Init(problem);
        }

        private void Init(Problem problem = null)
        {
            _problem = problem;
            var problemType = _problem?.Type;
            if (!string.IsNullOrEmpty(problemType))
            {
                if (problemType.StartsWith(Problem.StandardProblemTypeNamespace))
                {
                    if (Enum.TryParse(
                        problemType.Substring(Problem.StandardProblemTypeNamespace.Length), 
                        true,
                        out ProblemType pt))
                    {
                        ProblemType = pt;
                    };
                }
            }
        }

        public ProblemType ProblemType { get; private set; } = ProblemType.Unknown;

        public string ProblemTypeRaw => _problem?.Type;

        public string ProblemDetail => _problem?.Detail;

        public int ProblemStatus => _problem?.Status ?? -1;
    }
}
