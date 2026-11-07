using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken)
    {
        if (query.MinPrice is < 0 || query.MaxPrice is < 0 ||
            query.MinPrice is decimal minPrice && query.MaxPrice is decimal maxPrice && minPrice > maxPrice)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid product filters",
                Detail = "Prices must be non-negative and minPrice cannot exceed maxPrice."
            });
        }

        if (query.Sort is not null && query.Sort is not ("newest" or "price_asc" or "price_desc" or "name"))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid sort order",
                Detail = "sort must be newest, price_asc, price_desc, or name."
            });
        }

        return Ok(await productService.GetProductsAsync(query, cancellationToken));
    }

    [HttpGet("by-ids")]
    [ProducesResponseType<IReadOnlyList<ProductListDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductListDto>>> GetProductsByIds(
        [FromQuery, Required] string? ids,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Product IDs are required",
                Detail = "Provide one or more comma-separated positive product IDs."
            });
        }

        var parsedIds = new List<int>();
        foreach (var idText in ids.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(idText, out var id) || id <= 0)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid product IDs",
                    Detail = "Product IDs must be positive integers."
                });
            }
            parsedIds.Add(id);
        }

        return Ok(await productService.GetProductsByIdsAsync(parsedIds, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailsDto>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }
}