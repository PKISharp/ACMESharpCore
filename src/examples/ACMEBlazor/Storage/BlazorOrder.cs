using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEBlazor.Storage
{
    public class BlazorOrder
    {
        public int Id { get; set; }

        [Required]
        public BlazorAccount Account { get; set; }

        [Required]
        public OrderDetails Details { get; set; }
    }
}
