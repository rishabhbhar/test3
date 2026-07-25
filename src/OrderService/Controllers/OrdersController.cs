using System.Security.Claims;
using Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderProcessingService _service;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderProcessingService service, ILogger<OrdersController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Place a new order for one or more products. Any authenticated user.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetUserId();
            var token = GetBearerToken();

            if (string.IsNullOrWhiteSpace(token))
            {
                // Fail fast if caller did not present a bearer token
                return Unauthorized(new { error = "Missing bearer token" });
            }

            var order = await _service.CreateOrderAsync(userId, token, dto);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
        }

        /// <summary>Get the current user's own order history.</summary>
        [HttpGet("my-orders")]
        [ProducesResponseType(typeof(PagedResult<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<OrderResponseDto>>> GetMyOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var result = await _service.GetMyOrdersAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>Admin-only: list every order across all users.</summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(PagedResult<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<OrderResponseDto>>> GetAllOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _service.GetAllOrdersAsync(pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>Get a single order by ID. Owner or Admin only.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponseDto>> GetOrderById([FromRoute] Guid id)
        {
            var order = await _service.GetOrderByIdAsync(id, GetUserId(), User.IsInRole(Roles.Admin));
            if (order is null)
            {
                return NotFound(new { message = $"Order '{id}' was not found." });
            }

            return Ok(order);
        }

        /// <summary>Cancel an order. Restores stock for every line item. Owner or Admin only.</summary>
        [HttpPatch("{id:guid}/cancel")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponseDto>> CancelOrder([FromRoute] Guid id)
        {
            var result = await _service.CancelOrderAsync(id, GetUserId(), User.IsInRole(Roles.Admin));
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user identity in token.");
            }

            return userId;
        }

        private string GetBearerToken()
        {
            var header = Request.Headers.Authorization.ToString();
            return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : header;
        }
    }
}
