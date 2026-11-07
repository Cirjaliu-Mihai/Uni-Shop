using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;

namespace UniShop.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryListDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryListDto>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await categoryService.GetCategoriesAsync(cancellationToken));
}