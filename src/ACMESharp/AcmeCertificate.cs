using System;
using System.Collections.Generic;
using System.Text;
using ACMESharp.HTTP;

namespace ACMESharp
{
    public class AcmeCertificate
    {
        public byte[] Certificate { get; set; }
        public LinkCollection Links { get; set; }
    }
}
