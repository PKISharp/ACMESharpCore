using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEForms.Storage
{
    public class DbOrder
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public OrderDetails Details { get; set; }
    }
}
