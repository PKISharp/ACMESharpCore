using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;

namespace ACMEForms.Storage
{
    public class DbOrder
    {
        public int Id { get; set; }

        /// <summary>
        /// We cache the first Order URL we get because subsequent
        /// refreshes of the Order don't return it in the response.
        /// </summary>
        public string FirstOrderUrl { get; set; }

        public OrderDetails Details { get; set; }

        public DbAuthz[] Authorizations { get; set; }
    }
}
