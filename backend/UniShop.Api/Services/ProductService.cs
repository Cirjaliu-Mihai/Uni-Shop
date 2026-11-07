using Microsoft.EntityFrameworkCore;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;

namespace UniShop.Api.Services;

public sealed class ProductService(AppDbContext dbContext) : IProductService
{
    public async Task<PagedResult<ProductListDto>> GetProductsAsync(
        ProductQuery query,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = $"%{EscapeLikePattern(query.Search.Trim())}%";
            products = products.Where(product => EF.Functions.ILike(product.Name, search, "\\"));
        }

        if (query.CategoryId is int categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (query.MinPrice is decimal minPrice)
        {
            products = products.Where(product => product.Price >= minPrice);
        }

        if (query.MaxPrice is decimal maxPrice)
        {
            products = products.Where(product => product.Price <= maxPrice);
        }

        if (query.InStock is true)
        {
            products = products.Where(product => product.Stock > 0);
        }
        else if (query.InStock is false)
        {
            products = products.Where(product => product.Stock == 0);
        }

        var totalCount = await products.CountAsync(cancellationToken);
        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? 12, 1, 50);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var skip = (long)(page - 1) * pageSize;

        var orderedProducts = ApplySort(products, query.Sort);
        var items = skip >= totalCount
            ? []
            : await orderedProducts
                .Skip((int)skip)
                .Take(pageSize)
                .Select(product => new ProductListDto(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Stock,
                    product.ImageUrl,
                    product.CategoryId,
                    product.Category.Name,
                    product.CreatedAt))
                .ToListAsync(cancellationToken);

        return new PagedResult<ProductListDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<ProductDetailsDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id && product.IsActive)
            .Select(product => new ProductDetailsDto(
                product.Id,
                product.Name,
                product.Description,
                product.Specifications,
                product.Price,
                product.Stock,
                product.ImageUrl,
                product.CategoryId,
                product.Category.Name,
                product.Supplier.Name,
                product.DeliveryMethods
                    .OrderBy(method => method.Name)
                    .Select(method => new DeliveryMethodDto(method.Id, method.Name, method.Price, method.EstimatedDays))
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductListDto>> GetProductsByIdsAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive && ids.Contains(product.Id))
            .Select(product => new ProductListDto(
                product.Id,
                product.Name,
                product.Price,
                product.Stock,
                product.ImageUrl,
                product.CategoryId,
                product.Category.Name,
                product.CreatedAt))
            .ToListAsync(cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        return ids.Distinct()
            .Where(productsById.ContainsKey)
            .Select(id => productsById[id])
            .ToArray();
    }

    private static IOrderedQueryable<Product> ApplySort(IQueryable<Product> products, string? sort) => sort switch
    {
        "price_asc" => products.OrderBy(product => product.Price).ThenBy(product => product.Id),
        "price_desc" => products.OrderByDescending(product => product.Price).ThenBy(product => product.Id),
        "name" => products.OrderBy(product => product.Name).ThenBy(product => product.Id),
        _ => products.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id)
    };

    private static string EscapeLikePattern(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}