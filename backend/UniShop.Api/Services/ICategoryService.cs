using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListDto>> GetCategoriesAsync(CancellationToken cancellationToken);
}