using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ACMESharp.Protocol.Resources
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.2
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
    /// </summary>
    public class Account
    {
        public string Id { get; set; }

        public object Key { get; set; }

        public string[] Contact { get; set; }

        public string Status { get; set; }

        public bool? TermsOfServiceAgreed { get; set; }

        public string Orders { get; set; }

        // TODO: are these standard or specific to LE?
        //    "agreement": "https://letsencrypt.org/documents/LE-SA-v1.2-November-15-2017.pdf",
        //    "initialIp": "50.235.30.49",
        //    "createdAt": "2018-05-02T22:23:30Z",
        public string InitialIp { get; set; }
        public string CreatedAt { get; set; }
        public string Agreement { get; set; }
    }
}