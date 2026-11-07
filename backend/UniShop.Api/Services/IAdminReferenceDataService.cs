using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IAdminReferenceDataService
{
    Task<IReadOnlyList<CategoryAdminDto>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<CategoryAdminDto?> GetCategoryAsync(int id, CancellationToken cancellationToken);
    Task<CategoryAdminDto> CreateCategoryAsync(CategoryWriteRequest request, CancellationToken cancellationToken);
    Task<CategoryAdminDto?> UpdateCategoryAsync(int id, CategoryWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierAdminDto>> GetSuppliersAsync(CancellationToken cancellationToken);
    Task<SupplierAdminDto?> GetSupplierAsync(int id, CancellationToken cancellationToken);
    Task<SupplierAdminDto> CreateSupplierAsync(SupplierWriteRequest request, CancellationToken cancellationToken);
    Task<SupplierAdminDto?> UpdateSupplierAsync(int id, SupplierWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliveryMethodAdminDto>> GetDeliveryMethodsAsync(CancellationToken cancellationToken);
    Task<DeliveryMethodAdminDto?> GetDeliveryMethodAsync(int id, CancellationToken cancellationToken);
    Task<DeliveryMethodAdminDto> CreateDeliveryMethodAsync(DeliveryMethodWriteRequest request, CancellationToken cancellationToken);
    Task<DeliveryMethodAdminDto?> UpdateDeliveryMethodAsync(int id, DeliveryMethodWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteDeliveryMethodAsync(int id, CancellationToken cancellationToken);
}