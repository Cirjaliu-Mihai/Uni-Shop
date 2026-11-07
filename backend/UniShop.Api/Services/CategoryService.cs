using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class CategoryService(AppDbContext dbContext) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryListDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryListDto(
                category.Id,
                category.Name,
                category.Description,
                category.Products.Count(product => product.IsActive)))
            .ToListAsync(cancellationToken);
}