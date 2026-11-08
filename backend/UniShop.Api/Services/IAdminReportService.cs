using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IAdminReportService
{
    Task<IReadOnlyList<SalesReportPointDto>> GetSalesAsync(SalesReportQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<PopularProductReportDto>> GetPopularProductsAsync(PopularProductsReportQuery query, CancellationToken cancellationToken);
}