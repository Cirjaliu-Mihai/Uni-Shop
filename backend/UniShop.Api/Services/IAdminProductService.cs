using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IAdminProductService
{
    Task<PagedResult<AdminProductListDto>> GetProductsAsync(AdminProductQuery query, CancellationToken cancellationToken);
    Task<AdminProductDto?> GetProductAsync(int id, CancellationToken cancellationToken);
    Task<Dictionary<string, string[]>> ValidateProductAsync(ProductWriteRequest request, CancellationToken cancellationToken);
    Task<AdminProductDto> CreateProductAsync(ProductWriteRequest request, CancellationToken cancellationToken);
    Task<AdminProductDto?> UpdateProductAsync(int id, ProductWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken);
}