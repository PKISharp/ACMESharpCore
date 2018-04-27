namespace ACMESharp.Protocol.Messages
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.2.1
    /// </summary>
    public class OrdersResponse
    {
        public string[] Orders { get; set; }
    }
}