using ACMESharp.Protocol;

namespace ACMESharp.MockServer.Storage
{
    public class DbOrder
    {
        public int Id { get; set; }

        public int AccountId { get; set; }

        public string Url { get; set; }

        public OrderDetails Details { get; set; }
    }
}