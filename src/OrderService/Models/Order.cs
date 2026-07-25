namespace OrderService.Models
{
    public static class OrderStatus
    {
        public const string Created = "CREATED";
        public const string Confirmed = "CONFIRMED";
        public const string Cancelled = "CANCELLED";
    }

    public class Order
    {
        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        /// <summary>CREATED, CONFIRMED, or CANCELLED - matches chk_order_status.</summary>
        public string OrderStatus { get; set; } = Models.OrderStatus.Created;

        public DateTime CreatedAt { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public Guid OrderItemId { get; set; }

        public Guid OrderId { get; set; }

        public Guid ProductId { get; set; }

        public int Quantity { get; set; }

        public Order? Order { get; set; }
    }
}
