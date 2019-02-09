using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEForms
{
    public class AppState
    {
        public AccountDetails Account { get; set; }

        public OrderDetails[] Orders { get; set; }
    }
}
