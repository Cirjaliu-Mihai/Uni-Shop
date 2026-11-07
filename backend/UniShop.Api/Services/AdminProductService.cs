using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class AdminProductService(AppDbContext dbContext) : IAdminProductService
{
    public async Task<PagedResult<AdminProductListDto>> GetProductsAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{EscapeLikePattern(query.Search.Trim())}%";
            products = products.Where(product => EF.Functions.ILike(product.Name, search, "\\"));
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? 20, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (long)(page - 1) * pageSize;
        var items = skip >= totalCount
            ? []
            : await products
                .OrderByDescending(product => product.UpdatedAt)
                .ThenByDescending(product => product.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(product => new AdminProductListDto(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Stock,
                    product.IsActive,
                    product.Category.Name,
                    product.Supplier.Name,
                    product.UpdatedAt))
                .ToListAsync(cancellationToken);

        return new PagedResult<AdminProductListDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<AdminProductDto?> GetProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Supplier)
            .Include(item => item.DeliveryMethods)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return product is null ? null : ToDto(product);
    }

    public async Task<Dictionary<string, string[]>> ValidateProductAsync(
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = [];
                errors[key] = messages;
            }
            messages.Add(message);
        }

        var specifications = request.Specifications ?? [];
        if (specifications.Any(specification => string.IsNullOrWhiteSpace(specification.Key)))
        {
            AddError(nameof(request.Specifications), "Specification keys cannot be empty.");
        }
        if (specifications
            .Where(specification => !string.IsNullOrWhiteSpace(specification.Key))
            .GroupBy(specification => specification.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            AddError(nameof(request.Specifications), "Specification keys must be unique, ignoring case.");
        }

        if (!await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken))
        {
            AddError(nameof(request.CategoryId), "The selected category does not exist.");
        }
        if (!await dbContext.Suppliers.AnyAsync(supplier => supplier.Id == request.SupplierId, cancellationToken))
        {
            AddError(nameof(request.SupplierId), "The selected supplier does not exist.");
        }

        var deliveryMethodIds = request.DeliveryMethodIds ?? [];
        if (deliveryMethodIds.Length == 0)
        {
            AddError(nameof(request.DeliveryMethodIds), "Select at least one delivery method.");
        }
        else
        {
            if (deliveryMethodIds.Distinct().Count() != deliveryMethodIds.Length)
            {
                AddError(nameof(request.DeliveryMethodIds), "Delivery methods cannot be selected more than once.");
            }

            var existingMethodCount = await dbContext.DeliveryMethods
                .CountAsync(method => deliveryMethodIds.Contains(method.Id), cancellationToken);
            if (existingMethodCount != deliveryMethodIds.Distinct().Count())
            {
                AddError(nameof(request.DeliveryMethodIds), "One or more selected delivery methods do not exist.");
            }
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }

    public async Task<AdminProductDto> CreateProductAsync(ProductWriteRequest request, CancellationToken cancellationToken)
    {
        var product = new Product();
        ApplyRequest(product, request);
        var deliveryMethodIds = request.DeliveryMethodIds ?? Array.Empty<int>();
        product.DeliveryMethods = await dbContext.DeliveryMethods
            .Where(method => deliveryMethodIds.Contains(method.Id))
            .ToListAsync(cancellationToken);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetProductAsync(product.Id, cancellationToken))!;
    }

    public async Task<AdminProductDto?> UpdateProductAsync(
        int id,
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .Include(item => item.DeliveryMethods)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        ApplyRequest(product, request);
        var deliveryMethodIds = request.DeliveryMethodIds ?? Array.Empty<int>();
        var deliveryMethods = await dbContext.DeliveryMethods
            .Where(method => deliveryMethodIds.Contains(method.Id))
            .ToListAsync(cancellationToken);
        product.DeliveryMethods.Clear();
        foreach (var deliveryMethod in deliveryMethods)
        {
            product.DeliveryMethods.Add(deliveryMethod);
        }
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return false;
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private void ApplyRequest(Product product, ProductWriteRequest request)
    {
        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Specifications = (request.Specifications ?? [])
            .ToDictionary(specification => specification.Key.Trim(), specification => specification.Value, StringComparer.OrdinalIgnoreCase);
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        product.IsActive = request.IsActive;
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static AdminProductDto ToDto(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Specifications
            .OrderBy(specification => specification.Key)
            .Select(specification => new ProductSpecificationDto { Key = specification.Key, Value = specification.Value })
            .ToArray(),
        product.Price,
        product.Stock,
        product.ImageUrl,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt,
        product.CategoryId,
        product.Category.Name,
        product.SupplierId,
        product.Supplier.Name,
        product.DeliveryMethods.Select(method => method.Id).Order().ToArray());

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}