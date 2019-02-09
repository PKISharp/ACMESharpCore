using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Protocol;
using BlazorDB;

namespace ACMEBlazor.Storage
{
    public class Context : StorageContext
    {
        public StorageSet<BlazorAccount> Accounts { get; set; }
        public StorageSet<BlazorOrder> Orders { get; set; }
    }
}
