using InventoryService.Data;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly InventoryDbContext _context;

        public ProductRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> GetByIdAsync(Guid productId)
        {
            return await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, bool? isActive)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.ProductName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Product?> UpdateAsync(Guid productId, Action<Product> applyChanges)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product is null)
            {
                return null;
            }

            applyChanges(product);
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteAsync(Guid productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product is null)
            {
                return false;
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TryReduceStockAsync(Guid productId, int quantity)
        {
            // Single atomic UPDATE ... WHERE stock_qty >= @quantity AND is_active = 1.
            // Prevents overselling under concurrent requests without needing an explicit
            // transaction/row lock: the database evaluates the WHERE predicate and the
            // SET in one statement, so a second concurrent call sees the already-updated value.
            var rowsAffected = await _context.Products
                .Where(p => p.ProductId == productId && p.IsActive && p.StockQty >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQty, p => p.StockQty - quantity)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            return rowsAffected == 1;
        }

        public async Task<bool> RestoreStockAsync(Guid productId, int quantity)
        {
            var rowsAffected = await _context.Products
                .Where(p => p.ProductId == productId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQty, p => p.StockQty + quantity)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

            return rowsAffected == 1;
        }
    }
}
