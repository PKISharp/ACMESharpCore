using System.Threading.Tasks;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;

namespace ACMESharp.StateManagement
{
    public class AcmeAccount
    {
        public AcmeProtocolClient _acme;
        public AccountDetails _account;
        public string _keyType;
        public string _keyExport;

        public async Task Save()
        {
            // TODO: Save:
            // * Service endpoint
            // * _account
            // * _keyType
            // * _keyExport
        }

        // public static async Task<AcmeAccount> Load()
        // {

        // }
    }
}