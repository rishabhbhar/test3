using OrderService.Models;

namespace OrderService.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> AddAsync(Order order);
        Task<Order?> GetByIdAsync(Guid orderId);
        Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserAsync(Guid userId, int pageNumber, int pageSize);
        Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize);
        Task<bool> UpdateStatusAsync(Guid orderId, string newStatus);
    }
}
