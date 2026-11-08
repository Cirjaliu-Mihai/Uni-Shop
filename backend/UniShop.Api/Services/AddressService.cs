using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AddressService(AppDbContext dbContext) : IAddressService
{
    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(string userId, CancellationToken cancellationToken) =>
        await dbContext.Addresses.AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Id)
            .Select(address => ToDto(address))
            .ToListAsync(cancellationToken);

    public async Task<AddressDto> CreateAddressAsync(string userId, AddressWriteRequest request, CancellationToken cancellationToken)
    {
        var addresses = await dbContext.Addresses.Where(address => address.UserId == userId).ToListAsync(cancellationToken);
        var makeDefault = request.IsDefault || addresses.Count == 0;
        if (makeDefault)
        {
            foreach (var existingAddress in addresses) existingAddress.IsDefault = false;
        }

        var address = new Address { UserId = userId };
        Apply(address, request, makeDefault);
        dbContext.Addresses.Add(address);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(address);
    }

    public async Task<AddressDto?> UpdateAddressAsync(string userId, int id, AddressWriteRequest request, CancellationToken cancellationToken)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (address is null) return null;

        if (request.IsDefault)
        {
            await ClearDefaultAsync(userId, id, cancellationToken);
        }
        Apply(address, request, request.IsDefault);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(address);
    }

    public async Task<bool> DeleteAddressAsync(string userId, int id, CancellationToken cancellationToken)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (address is null) return false;

        dbContext.Addresses.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (address.IsDefault)
        {
            var replacement = await dbContext.Addresses
                .Where(item => item.UserId == userId)
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (replacement is not null)
            {
                replacement.IsDefault = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        return true;
    }

    public async Task<AddressDto?> SetDefaultAsync(string userId, int id, CancellationToken cancellationToken)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (address is null) return null;

        await ClearDefaultAsync(userId, id, cancellationToken);
        address.IsDefault = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(address);
    }

    private async Task ClearDefaultAsync(string userId, int exceptId, CancellationToken cancellationToken)
    {
        await dbContext.Addresses
            .Where(address => address.UserId == userId && address.Id != exceptId && address.IsDefault)
            .ExecuteUpdateAsync(setters => setters.SetProperty(address => address.IsDefault, false), cancellationToken);
    }

    private static void Apply(Address address, AddressWriteRequest request, bool isDefault)
    {
        address.Label = request.Label.Trim();
        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.Street = request.Street.Trim();
        address.City = request.City.Trim();
        address.County = request.County.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = request.Country.Trim();
        address.IsDefault = isDefault;
    }

    private static AddressDto ToDto(Address address) => new(
        address.Id,
        address.Label,
        address.FullName,
        address.Phone,
        address.Street,
        address.City,
        address.County,
        address.PostalCode,
        address.Country,
        address.IsDefault);
}