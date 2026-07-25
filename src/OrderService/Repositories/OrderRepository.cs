using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Order> AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetByUserAsync(Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.Orders.Include(o => o.Items).AsNoTracking().Where(o => o.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            var query = _context.Orders.Include(o => o.Items).AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> UpdateStatusAsync(Guid orderId, string newStatus)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order is null)
            {
                return false;
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
