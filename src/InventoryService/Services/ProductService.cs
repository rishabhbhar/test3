using Common.Middleware;
using InventoryService.DTOs;
using InventoryService.Models;
using InventoryService.Repositories;

namespace InventoryService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repository, ILogger<ProductService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = dto.ProductName.Trim(),
                StockQty = dto.StockQty,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(product);
            _logger.LogInformation("Created product {ProductId} ({ProductName}) with stock {StockQty}",
                created.ProductId, created.ProductName, created.StockQty);

            return ToResponseDto(created);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(Guid productId)
        {
            var product = await _repository.GetByIdAsync(productId);
            return product is null ? null : ToResponseDto(product);
        }

        public async Task<PagedResult<ProductResponseDto>> ListProductsAsync(int pageNumber, int pageSize, bool? isActive)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _repository.GetAllAsync(pageNumber, pageSize, isActive);

            return new PagedResult<ProductResponseDto>
            {
                Items = items.Select(ToResponseDto),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<ProductResponseDto?> UpdateProductAsync(Guid productId, UpdateProductDto dto)
        {
            var updated = await _repository.UpdateAsync(productId, product =>
            {
                if (dto.ProductName is not null) product.ProductName = dto.ProductName.Trim();
                if (dto.StockQty.HasValue) product.StockQty = dto.StockQty.Value;
                if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
            });

            if (updated is null)
            {
                return null;
            }

            _logger.LogInformation("Updated product {ProductId}", productId);
            return ToResponseDto(updated);
        }

        public async Task<bool> DeleteProductAsync(Guid productId)
        {
            var result = await _repository.DeleteAsync(productId);
            if (result)
            {
                _logger.LogInformation("Deleted product {ProductId}", productId);
            }

            return result;
        }

        public async Task<ProductResponseDto> ReduceStockAsync(Guid productId, int quantity)
        {
            var product = await _repository.GetByIdAsync(productId)
                ?? throw new NotFoundException($"Product '{productId}' was not found.");

            if (!product.IsActive)
            {
                throw new BusinessRuleException($"Product '{product.ProductName}' is not active.");
            }

            var succeeded = await _repository.TryReduceStockAsync(productId, quantity);
            if (!succeeded)
            {
                throw new BusinessRuleException(
                    $"Insufficient stock for product '{product.ProductName}'. Requested {quantity}, available {product.StockQty}.");
            }

            _logger.LogInformation("Reduced stock for product {ProductId} by {Quantity}", productId, quantity);

            var refreshed = await _repository.GetByIdAsync(productId);
            return ToResponseDto(refreshed!);
        }

        public async Task<ProductResponseDto> RestoreStockAsync(Guid productId, int quantity)
        {
            var succeeded = await _repository.RestoreStockAsync(productId, quantity);
            if (!succeeded)
            {
                throw new NotFoundException($"Product '{productId}' was not found.");
            }

            _logger.LogInformation("Restored stock for product {ProductId} by {Quantity}", productId, quantity);

            var refreshed = await _repository.GetByIdAsync(productId);
            return ToResponseDto(refreshed!);
        }

        private static ProductResponseDto ToResponseDto(Product product) => new()
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            StockQty = product.StockQty,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
