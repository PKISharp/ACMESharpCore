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

        public string Contacts { get; set; }

        [Required]
        public string[] ContactEmails { get; set; }

        [Required]
        public DateTime? TosAgreed { get; set; }

        [Required]
        public string SignerExport { get; set; }

        [Required]
        public AccountDetails Details { get; set; }
    }
}
