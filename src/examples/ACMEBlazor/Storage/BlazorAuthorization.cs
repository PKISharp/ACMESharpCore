using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Protocol.Resources;

namespace ACMEBlazor.Storage
{
    public class BlazorAuthorization
    {
        public int Id { get; set; }

        //[Required]
        //public BlazorOrder Order { get; set; }

        [Required]
        public string Url { get; set; }

        [Required]
        public Authorization Details { get; set; }

        public Dns01ChallengeValidationDetails DnsChallengeDetails { get; set; }

        public Http01ChallengeValidationDetails HttpChallengeDetails { get; set; }
    }
}
