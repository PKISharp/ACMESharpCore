using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;

namespace ACMEBlazor.Storage
{
    public class BlazorOrder
    {
        public int Id { get; set; }

        public string DnsNames { get; set; }

        [Required]
        public BlazorAccount Account { get; set; }

        [Required]
        public OrderDetails Details { get; set; }

        public BlazorAuthorization[] Authorizations { get; set; }
    }
}
