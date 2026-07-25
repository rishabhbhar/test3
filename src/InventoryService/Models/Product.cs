namespace InventoryService.Models
{
    public class Product
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int StockQty { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
