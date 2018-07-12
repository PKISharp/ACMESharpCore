using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMEBlazor.Services
{
    public interface IRepository
    {
        Task<AccountDetails> GetAccount();
    }

    public class Repository : IRepository
    {
        public async Task<AccountDetails> GetAccount()
        {
            return null;
        }
    }
}
