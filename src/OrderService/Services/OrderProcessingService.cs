using Common.Middleware;
using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services
{
    public class OrderProcessingService : IOrderProcessingService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IInventoryServiceClient _inventoryClient;
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderProcessingService> _logger;

        public OrderProcessingService(
            IOrderRepository orderRepository,
            IInventoryServiceClient inventoryClient,
            OrderDbContext context,
            ILogger<OrderProcessingService> logger)
        {
            _orderRepository = orderRepository;
            _inventoryClient = inventoryClient;
            _context = context;
            _logger = logger;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(Guid userId, string callerBearerToken, CreateOrderDto dto)
        {
            static string MaskToken(string? t)
            {
                if (string.IsNullOrEmpty(t)) return "<none>";
                if (t.Length <= 10) return "***";
                return t.Substring(0, 6) + "..." + t.Substring(t.Length - 4);
            }

            _logger.LogInformation("Creating order for user {UserId} with {ItemCount} item(s). Token={Token}", userId, dto?.Items?.Count ?? 0, MaskToken(callerBearerToken));

            if (dto.Items is null || dto.Items.Count == 0)
            {
                throw new BusinessRuleException("An order must contain at least one item.");
            }

            // Merge duplicate product lines so we validate/reduce stock once per product.
            var mergedItems = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new CreateOrderItemDto { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            // ---- Step 1: Check stock (validate every line before mutating anything) ----
            foreach (var item in mergedItems)
            {
                _logger.LogDebug("Checking stock for product {ProductId} qty {Quantity}", item.ProductId, item.Quantity);
                var product = await _inventoryClient.GetProductAsync(item.ProductId, callerBearerToken)
                    ?? throw new NotFoundException($"Product '{item.ProductId}' was not found.");

                if (!product.IsActive)
                {
                    throw new BusinessRuleException($"Product '{product.ProductName}' is not active.");
                }

                if (product.StockQty < item.Quantity)
                {
                    throw new BusinessRuleException(
                        $"Insufficient stock for product '{product.ProductName}'. Requested {item.Quantity}, available {product.StockQty}.");
                }
            }

            // ---- Step 2: Deduct stock (saga: track what succeeded, compensate on failure) ----
            var reducedItems = new List<CreateOrderItemDto>();
            try
            {
                foreach (var item in mergedItems)
                {
                    _logger.LogDebug("Reducing stock for product {ProductId} qty {Quantity}", item.ProductId, item.Quantity);
                    await _inventoryClient.ReduceStockAsync(item.ProductId, item.Quantity);
                    reducedItems.Add(item);
                }
            }
            catch (Exception)
            {
                _logger.LogWarning("Stock reduction failed mid-order; compensating {Count} already-reduced item(s).", reducedItems.Count);
                await CompensateAsync(reducedItems);
                throw; // entire order is rejected
            }

            // ---- Step 3 & 4: Create order + items, mark CONFIRMED ----
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                UserId = userId,
                OrderStatus = OrderStatus.Created,
                CreatedAt = DateTime.UtcNow,
                Items = mergedItems.Select(i => new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.OrderStatus = OrderStatus.Confirmed;
                await _orderRepository.AddAsync(order);
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Failed to persist order after stock was already reduced; compensating stock.");
                // Compensate only for items that have actually been reduced
                await CompensateAsync(reducedItems);
                throw;
            }

            _logger.LogInformation("Order {OrderId} CONFIRMED for user {UserId} with {ItemCount} line item(s).",
                order.OrderId, userId, order.Items.Count);

            return ToResponseDto(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId, Guid requestingUserId, bool isAdmin)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
            {
                return null;
            }

            if (!isAdmin && order.UserId != requestingUserId)
            {
                // Users may only see their own orders - do not leak existence to other users.
                throw new ForbiddenException("You do not have access to this order.");
            }

            return ToResponseDto(order);
        }

        public async Task<PagedResult<OrderResponseDto>> GetMyOrdersAsync(Guid userId, int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Normalize(pageNumber, pageSize);
            var (items, totalCount) = await _orderRepository.GetByUserAsync(userId, pageNumber, pageSize);

            return new PagedResult<OrderResponseDto>
            {
                Items = items.Select(ToResponseDto),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<OrderResponseDto>> GetAllOrdersAsync(int pageNumber, int pageSize)
        {
            (pageNumber, pageSize) = Normalize(pageNumber, pageSize);
            var (items, totalCount) = await _orderRepository.GetAllAsync(pageNumber, pageSize);

            return new PagedResult<OrderResponseDto>
            {
                Items = items.Select(ToResponseDto),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<OrderResponseDto> CancelOrderAsync(Guid orderId, Guid requestingUserId, bool isAdmin)
        {
            var order = await _orderRepository.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order '{orderId}' was not found.");

            if (!isAdmin && order.UserId != requestingUserId)
            {
                throw new ForbiddenException("You do not have access to this order.");
            }

            if (order.OrderStatus == OrderStatus.Cancelled)
            {
                throw new BusinessRuleException("This order has already been cancelled.");
            }

            // Restore stock for every line item (best-effort; failures are logged, not fatal,
            // since the order must still transition to CANCELLED for the user).
            foreach (var item in order.Items)
            {
                await _inventoryClient.RestoreStockAsync(item.ProductId, item.Quantity);
            }

            await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);
            order.OrderStatus = OrderStatus.Cancelled;

            _logger.LogInformation("Order {OrderId} CANCELLED (requested by {UserId}, admin={IsAdmin})", orderId, requestingUserId, isAdmin);

            return ToResponseDto(order);
        }

        private async Task CompensateAsync(List<CreateOrderItemDto> itemsToRestore)
        {
            foreach (var item in itemsToRestore)
            {
                try
                {
                    await _inventoryClient.RestoreStockAsync(item.ProductId, item.Quantity);
                }
                catch (Exception ex)
                {
                    // Compensation failures are logged loudly - this indicates a manual
                    // reconciliation may be needed, but must not mask the original error.
                    _logger.LogError(ex, "Failed to compensate stock for product {ProductId}.", item.ProductId);
                }
            }
        }

        private static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;
            return (pageNumber, pageSize);
        }

        private static OrderResponseDto ToResponseDto(Order order) => new()
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            OrderStatus = order.OrderStatus,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
