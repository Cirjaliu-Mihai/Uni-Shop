using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/products")]
public sealed class AdminProductsController(IAdminProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<AdminProductListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminProductListDto>>> GetProducts(
        [FromQuery] AdminProductQuery query,
        CancellationToken cancellationToken) =>
        Ok(await productService.GetProductsAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminProductDto>> CreateProduct(
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var errors = await productService.ValidateProductAsync(request, cancellationToken);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var product = await productService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(
        int id,
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var errors = await productService.ValidateProductAsync(request, cancellationToken);
        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var product = await productService.UpdateProductAsync(id, request, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken) =>
        await productService.DeleteProductAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPatch("{id:int}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateProduct(int id, CancellationToken cancellationToken) =>
        await productService.DeactivateProductAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPatch("{id:int}/stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(
        int id,
        UpdateProductStockRequest request,
        CancellationToken cancellationToken) =>
        await productService.UpdateStockAsync(id, request.Stock!.Value, cancellationToken) ? NoContent() : NotFound();
}