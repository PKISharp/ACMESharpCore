using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMEBlazor.Storage;

namespace ACMEBlazor
{
    public class AppState
    {
        public static readonly string AccountKey = $"{nameof(ACMEBlazor)}-{nameof(BlazorAccount)}";
        public static readonly string OrderKey = $"{nameof(ACMEBlazor)}-{nameof(BlazorOrder)}:";

        public static readonly char[] LineSeps = "\r\n".ToCharArray();

        public static readonly BlazorAccount[] EmptyAccounts = new BlazorAccount[0];
        public static readonly BlazorOrder[] EmptyOrders = new BlazorOrder[0];

        public BlazorAccount Account { get; set; }

        public BlazorOrder[] Orders { get; set; }
    }
}
