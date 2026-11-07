using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AdminReferenceDataService(AppDbContext dbContext) : IAdminReferenceDataService
{
    public async Task<IReadOnlyList<CategoryAdminDto>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryAdminDto(
                category.Id,
                category.Name,
                category.Description,
                category.Products.Count()))
            .ToListAsync(cancellationToken);

    public async Task<CategoryAdminDto?> GetCategoryAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryAdminDto(
                category.Id,
                category.Name,
                category.Description,
                category.Products.Count()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CategoryAdminDto> CreateCategoryAsync(CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(dbContext.Categories.Where(category => category.Name.ToLower() == name.ToLower()), "A category with this name already exists.", cancellationToken);
        var category = new Category { Name = name, Description = Normalize(request.Description) };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CategoryAdminDto(category.Id, category.Name, category.Description, 0);
    }

    public async Task<CategoryAdminDto?> UpdateCategoryAsync(int id, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null) return null;

        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(
            dbContext.Categories.Where(existing => existing.Id != id && existing.Name.ToLower() == name.ToLower()),
            "A category with this name already exists.",
            cancellationToken);
        category.Name = name;
        category.Description = Normalize(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetCategoryAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.FindAsync([id], cancellationToken);
        if (category is null) return false;
        if (await dbContext.Products.AnyAsync(product => product.CategoryId == id, cancellationToken))
        {
            throw new AdminConflictException("Cannot delete this category while products are assigned to it.");
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SupplierAdminDto>> GetSuppliersAsync(CancellationToken cancellationToken) =>
        await dbContext.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierAdminDto(
                supplier.Id,
                supplier.Name,
                supplier.ContactEmail,
                supplier.Phone,
                supplier.Products.Count()))
            .ToListAsync(cancellationToken);

    public async Task<SupplierAdminDto?> GetSupplierAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Suppliers.AsNoTracking()
            .Where(supplier => supplier.Id == id)
            .Select(supplier => new SupplierAdminDto(
                supplier.Id,
                supplier.Name,
                supplier.ContactEmail,
                supplier.Phone,
                supplier.Products.Count()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SupplierAdminDto> CreateSupplierAsync(SupplierWriteRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(dbContext.Suppliers.Where(supplier => supplier.Name.ToLower() == name.ToLower()), "A supplier with this name already exists.", cancellationToken);
        var supplier = new Supplier
        {
            Name = name,
            ContactEmail = request.ContactEmail.Trim(),
            Phone = request.Phone.Trim()
        };
        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SupplierAdminDto(supplier.Id, supplier.Name, supplier.ContactEmail, supplier.Phone, 0);
    }

    public async Task<SupplierAdminDto?> UpdateSupplierAsync(int id, SupplierWriteRequest request, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FindAsync([id], cancellationToken);
        if (supplier is null) return null;

        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(
            dbContext.Suppliers.Where(existing => existing.Id != id && existing.Name.ToLower() == name.ToLower()),
            "A supplier with this name already exists.",
            cancellationToken);
        supplier.Name = name;
        supplier.ContactEmail = request.ContactEmail.Trim();
        supplier.Phone = request.Phone.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetSupplierAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteSupplierAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.FindAsync([id], cancellationToken);
        if (supplier is null) return false;
        if (await dbContext.Products.AnyAsync(product => product.SupplierId == id, cancellationToken))
        {
            throw new AdminConflictException("Cannot delete this supplier while products are assigned to it.");
        }

        dbContext.Suppliers.Remove(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<DeliveryMethodAdminDto>> GetDeliveryMethodsAsync(CancellationToken cancellationToken) =>
        await dbContext.DeliveryMethods.AsNoTracking()
            .OrderBy(method => method.Name)
            .Select(method => new DeliveryMethodAdminDto(
                method.Id,
                method.Name,
                method.Price,
                method.EstimatedDays,
                method.Products.Count()))
            .ToListAsync(cancellationToken);

    public async Task<DeliveryMethodAdminDto?> GetDeliveryMethodAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.DeliveryMethods.AsNoTracking()
            .Where(method => method.Id == id)
            .Select(method => new DeliveryMethodAdminDto(
                method.Id,
                method.Name,
                method.Price,
                method.EstimatedDays,
                method.Products.Count()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DeliveryMethodAdminDto> CreateDeliveryMethodAsync(DeliveryMethodWriteRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(dbContext.DeliveryMethods.Where(method => method.Name.ToLower() == name.ToLower()), "A delivery method with this name already exists.", cancellationToken);
        var method = new DeliveryMethod { Name = name, Price = request.Price, EstimatedDays = request.EstimatedDays };
        dbContext.DeliveryMethods.Add(method);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeliveryMethodAdminDto(method.Id, method.Name, method.Price, method.EstimatedDays, 0);
    }

    public async Task<DeliveryMethodAdminDto?> UpdateDeliveryMethodAsync(int id, DeliveryMethodWriteRequest request, CancellationToken cancellationToken)
    {
        var method = await dbContext.DeliveryMethods.FindAsync([id], cancellationToken);
        if (method is null) return null;

        var name = request.Name.Trim();
        await EnsureUniqueNameAsync(
            dbContext.DeliveryMethods.Where(existing => existing.Id != id && existing.Name.ToLower() == name.ToLower()),
            "A delivery method with this name already exists.",
            cancellationToken);
        method.Name = name;
        method.Price = request.Price;
        method.EstimatedDays = request.EstimatedDays;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetDeliveryMethodAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteDeliveryMethodAsync(int id, CancellationToken cancellationToken)
    {
        var method = await dbContext.DeliveryMethods.FindAsync([id], cancellationToken);
        if (method is null) return false;
        if (await dbContext.Products.AnyAsync(product => product.DeliveryMethods.Any(deliveryMethod => deliveryMethod.Id == id), cancellationToken))
        {
            throw new AdminConflictException("Cannot delete this delivery method while products use it.");
        }

        dbContext.DeliveryMethods.Remove(method);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task EnsureUniqueNameAsync<T>(IQueryable<T> matches, string message, CancellationToken cancellationToken)
    {
        if (await matches.AnyAsync(cancellationToken))
        {
            throw new AdminConflictException(message);
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}