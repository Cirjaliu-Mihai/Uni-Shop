using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/reports")]
public sealed class AdminReportsController(IAdminReportService reportService) : ControllerBase
{
    [HttpGet("sales")]
    [ProducesResponseType<IReadOnlyList<SalesReportPointDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesReportPointDto>>> GetSales(
        [FromQuery] SalesReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.From > query.To)
        {
            ModelState.AddModelError(nameof(query.From), "From must be earlier than or equal to To.");
            return ValidationProblem(ModelState);
        }
        return Ok(await reportService.GetSalesAsync(query, cancellationToken));
    }

    [HttpGet("popular-products")]
    [ProducesResponseType<IReadOnlyList<PopularProductReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PopularProductReportDto>>> GetPopularProducts(
        [FromQuery] PopularProductsReportQuery query,
        CancellationToken cancellationToken)
    {
        if (query.From > query.To)
        {
            ModelState.AddModelError(nameof(query.From), "From must be earlier than or equal to To.");
            return ValidationProblem(ModelState);
        }
        return Ok(await reportService.GetPopularProductsAsync(query, cancellationToken));
    }
}