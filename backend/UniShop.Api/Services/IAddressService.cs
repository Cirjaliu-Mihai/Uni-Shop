using UniShop.Api.Contracts;

namespace UniShop.Api.Services;

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(string userId, CancellationToken cancellationToken);
    Task<AddressDto> CreateAddressAsync(string userId, AddressWriteRequest request, CancellationToken cancellationToken);
    Task<AddressDto?> UpdateAddressAsync(string userId, int id, AddressWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAddressAsync(string userId, int id, CancellationToken cancellationToken);
    Task<AddressDto?> SetDefaultAsync(string userId, int id, CancellationToken cancellationToken);
}