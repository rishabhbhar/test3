using OrderService.DTOs;

namespace OrderService.Services
{
    public interface IOrderProcessingService
    {
        Task<OrderResponseDto> CreateOrderAsync(Guid userId, string callerBearerToken, CreateOrderDto dto);

        Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId, Guid requestingUserId, bool isAdmin);

        Task<PagedResult<OrderResponseDto>> GetMyOrdersAsync(Guid userId, int pageNumber, int pageSize);

        Task<PagedResult<OrderResponseDto>> GetAllOrdersAsync(int pageNumber, int pageSize);

        Task<OrderResponseDto> CancelOrderAsync(Guid orderId, Guid requestingUserId, bool isAdmin);
    }
}
