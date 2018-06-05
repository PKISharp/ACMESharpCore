namespace ACMESharp.Protocol.Model
{
    /// <summary>
    /// An aggregation of Order details including resource payload and ancillary,
    /// associated data.
    /// </summary>
    /// <remarks>
    /// This represents a superset of details that are included in responses
    /// to several ACME operations regarding an ACME Order, such as 
    /// Order creation and finalization.
    /// </remarks>
    public class OrderDetails
    {
        public Order Payload { get; set; }

        public string OrderUrl { get; set; }
    }
}