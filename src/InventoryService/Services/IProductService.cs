using InventoryService.DTOs;

namespace InventoryService.Services
{
    public interface IProductService
    {
        Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);
        Task<ProductResponseDto?> GetProductByIdAsync(Guid productId);
        Task<PagedResult<ProductResponseDto>> ListProductsAsync(int pageNumber, int pageSize, bool? isActive);
        Task<ProductResponseDto?> UpdateProductAsync(Guid productId, UpdateProductDto dto);
        Task<bool> DeleteProductAsync(Guid productId);
        Task<ProductResponseDto> ReduceStockAsync(Guid productId, int quantity);
        Task<ProductResponseDto> RestoreStockAsync(Guid productId, int quantity);
    }
}
