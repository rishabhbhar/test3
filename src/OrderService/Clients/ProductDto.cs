namespace OrderService.Clients
{
    public class ProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int StockQty { get; set; }
        public bool IsActive { get; set; }
    }

    public class StockAdjustmentRequest
    {
        public int Quantity { get; set; }
    }
}
