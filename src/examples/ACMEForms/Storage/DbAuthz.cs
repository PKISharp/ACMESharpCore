using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Protocol.Resources;

namespace ACMEForms.Storage
{
    public class DbAuthz
    {
        public int Id { get; set; }

        public string Url { get; set; }

        public Authorization Details { get; set; }

        public Dns01ChallengeValidationDetails DnsChallenge { get; set; }

        public Http01ChallengeValidationDetails HttpChallenge { get; set; }

        public Challenge[] MiscChallenges { get; set; }
    }
}
