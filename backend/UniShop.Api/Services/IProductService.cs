using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IProductService
{
    Task<PagedResult<ProductListDto>> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ProductDetailsDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductListDto>> GetProductsByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken);
}