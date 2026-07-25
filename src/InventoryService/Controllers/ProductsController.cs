using Common.Constants;
using InventoryService.DTOs;
using InventoryService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    [Authorize] // every endpoint requires a valid JWT unless overridden below
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService service, ILogger<ProductsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Create a new product. Admin only.</summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var created = await _service.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProductById), new { id = created.ProductId }, created);
        }

        /// <summary>List products with pagination. Any authenticated user.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ProductResponseDto>>> ListProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isActive = null)
        {
            var result = await _service.ListProductsAsync(pageNumber, pageSize, isActive);
            return Ok(result);
        }

        /// <summary>Get a product by ID. Any authenticated user.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponseDto>> GetProductById([FromRoute] Guid id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product is null)
            {
                return NotFound(new { message = $"Product '{id}' was not found." });
            }

            return Ok(product);
        }

        /// <summary>Update a product (name, stock, active flag). Admin only.</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponseDto>> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _service.UpdateProductAsync(id, dto);
            if (updated is null)
            {
                return NotFound(new { message = $"Product '{id}' was not found." });
            }

            return Ok(updated);
        }

        /// <summary>Delete a product. Admin only.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
        {
            var deleted = await _service.DeleteProductAsync(id);
            if (!deleted)
            {
                return NotFound(new { message = $"Product '{id}' was not found." });
            }

            return NoContent();
        }

        /// <summary>
        /// Reduce stock for a product. Allowed for: an Admin calling directly, OR
        /// OrderService calling internally (with the X-Internal-Api-Key header) on
        /// behalf of a User placing an order. See Common.Security.AdminOrInternalServiceHandler.
        /// </summary>
        [HttpPost("{id:guid}/reduce_stock")]
        [Authorize(Policy = InternalAuthDefaults.PolicyName)]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponseDto>> ReduceStock([FromRoute] Guid id, [FromBody] ReduceStockDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _service.ReduceStockAsync(id, dto.Quantity);
            return Ok(result);
        }

        /// <summary>
        /// Restore (increase) stock for a product - used when OrderService cancels an
        /// order and needs to roll back the earlier stock reduction. Same access rule
        /// as reduce_stock: Admin, or a trusted internal service call.
        /// </summary>
        [HttpPost("{id:guid}/restore_stock")]
        [Authorize(Policy = InternalAuthDefaults.PolicyName)]
        [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductResponseDto>> RestoreStock([FromRoute] Guid id, [FromBody] RestoreStockDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var result = await _service.RestoreStockAsync(id, dto.Quantity);
            return Ok(result);
        }
    }
}
