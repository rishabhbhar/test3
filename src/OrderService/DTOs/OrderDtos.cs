using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class CreateOrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        [Required, MinLength(1, ErrorMessage = "An order must contain at least one item.")]
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public Guid OrderItemId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
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
