using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEBlazor.Storage
{
    public class BlazorAccount
    {
        public int Id { get; set; }

        [Required]
        public AccountDetails Details { get; set; }
    }
}
