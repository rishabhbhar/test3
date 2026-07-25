using InventoryService.Models;

namespace InventoryService.Repositories
{
    public interface IProductRepository
    {
        Task<Product> AddAsync(Product product);
        Task<Product?> GetByIdAsync(Guid productId);
        Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, bool? isActive);
        Task<Product?> UpdateAsync(Guid productId, Action<Product> applyChanges);
        Task<bool> DeleteAsync(Guid productId);

        /// <summary>Atomically reduces stock only if sufficient stock is available. Returns false if insufficient.</summary>
        Task<bool> TryReduceStockAsync(Guid productId, int quantity);

        /// <summary>Atomically increases stock (used to restore stock on order cancellation).</summary>
        Task<bool> RestoreStockAsync(Guid productId, int quantity);
    }
}
