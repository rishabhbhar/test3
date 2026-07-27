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

        
        Task<bool> TryReduceStockAsync(Guid productId, int quantity);

        
        Task<bool> RestoreStockAsync(Guid productId, int quantity);
    }
}
