using ACMESharp.Protocol;

namespace ACMESharp.MockServer.Storage
{
    public class DbAccount
    {
        public int Id { get; set; }

        public AccountDetails Details { get; set; }
    }
}