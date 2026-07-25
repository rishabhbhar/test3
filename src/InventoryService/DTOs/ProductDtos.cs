using System.ComponentModel.DataAnnotations;

namespace InventoryService.DTOs
{
    public class CreateProductDto
    {
        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "StockQty must be zero or greater.")]
        public int StockQty { get; set; }
    }

    public class UpdateProductDto
    {
        [MaxLength(150)]
        public string? ProductName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "StockQty must be zero or greater.")]
        public int? StockQty { get; set; }

        public bool? IsActive { get; set; }
    }

    public class ReduceStockDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }
    }

    public class RestoreStockDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }
    }

    public class ProductResponseDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int StockQty { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
