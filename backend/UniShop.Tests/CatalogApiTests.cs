using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;
using Xunit;

namespace UniShop.Tests;

public class CatalogApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Photo_migration_fills_missing_sample_image_without_overwriting_custom_image()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var migrator = db.Database.GetService<IMigrator>();
        var laptop = await db.Products.AsNoTracking().SingleAsync(product => product.Name == "North 14 Laptop");
        var phone = await db.Products.AsNoTracking().SingleAsync(product => product.Name == "Everyday Phone");
        try
        {
            await migrator.MigrateAsync("20261107215937_AddAddressesOrdersAndPayments");
            await db.Products.Where(product => product.Id == laptop.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.ImageUrl, (string?)null));
            await db.Products.Where(product => product.Id == phone.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.ImageUrl, "https://example.test/custom.jpg"));

            await migrator.MigrateAsync();
            var images = await db.Products.AsNoTracking()
                .Where(product => product.Id == laptop.Id || product.Id == phone.Id)
                .ToDictionaryAsync(product => product.Id, product => product.ImageUrl);

            Assert.StartsWith("https://images.unsplash.com/photo-1496181133206-80ce9b88a853", images[laptop.Id]);
            Assert.Equal("https://example.test/custom.jpg", images[phone.Id]);
        }
        finally
        {
            await migrator.MigrateAsync();
            await db.Products.Where(product => product.Id == laptop.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.ImageUrl, laptop.ImageUrl));
            await db.Products.Where(product => product.Id == phone.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.ImageUrl, phone.ImageUrl));
        }
    }

    [Theory]
    [InlineData("North 14 Laptop", "photo-1496181133206-80ce9b88a853")]
    [InlineData("Everyday Phone", "photo-1511707171634-5f897ff02aa9")]
    [InlineData("Studio Headphones", "photo-1505740420928-5e560c06d30e")]
    public async Task Sample_product_has_a_direct_unsplash_photo_url(string name, string photoId)
    {
        using var client = fixture.Factory.CreateClient();
        var products = await GetProductsAsync(client, $"search={Uri.EscapeDataString(name)}");
        var product = Assert.Single(products.Items, item => item.Name == name);
        Assert.StartsWith($"https://images.unsplash.com/{photoId}", product.ImageUrl);
    }

    [Fact]
    public async Task Categories_include_active_product_counts()
    {
        using var client = fixture.Factory.CreateClient();
        var categories = await client.GetFromJsonAsync<List<CategoryListDto>>("/api/categories");

        Assert.NotNull(categories);
        Assert.Equal(8, categories.Count);
        Assert.Equal(3, categories.Single(category => category.Name == "Computers").ProductCount);
        Assert.Equal(3, categories.Single(category => category.Name == "Home Tech").ProductCount);
        Assert.Equal(3, categories.Single(category => category.Name == "Gaming").ProductCount);
        Assert.Equal(3, categories.Single(category => category.Name == "Wearables").ProductCount);
        Assert.Equal(3, categories.Single(category => category.Name == "Photography").ProductCount);
        Assert.All(categories, category => Assert.StartsWith("https://images.unsplash.com/photo-", category.ImageUrl));

        var products = await GetProductsAsync(client, "pageSize=50");
        Assert.Equal(24, products.Items.Count);
        Assert.All(products.Items, product => Assert.StartsWith("https://images.unsplash.com/photo-", product.ImageUrl));
    }

    [Fact]
    public async Task Search_filter_is_case_insensitive()
    {
        using var client = fixture.Factory.CreateClient();
        var result = await GetProductsAsync(client, "search=lApToP");

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, product => product.Name == "North 14 Laptop");
        Assert.Contains(result.Items, product => product.Name == "North 16 Laptop");
    }

    [Fact]
    public async Task Category_filter_returns_only_matching_products()
    {
        using var client = fixture.Factory.CreateClient();
        var categories = await client.GetFromJsonAsync<List<CategoryListDto>>("/api/categories");
        var audioId = categories!.Single(category => category.Name == "Audio").Id;
        var result = await GetProductsAsync(client, $"categoryId={audioId}");

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, product => Assert.Equal(audioId, product.CategoryId));
    }

    [Fact]
    public async Task New_category_filter_returns_its_three_sample_products()
    {
        using var client = fixture.Factory.CreateClient();
        var categories = await client.GetFromJsonAsync<List<CategoryListDto>>("/api/categories");
        var wearablesId = categories!.Single(category => category.Name == "Wearables").Id;
        var result = await GetProductsAsync(client, $"categoryId={wearablesId}");

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, product => Assert.Equal("Wearables", product.CategoryName));
    }

    [Fact]
    public async Task Minimum_price_filter_is_inclusive()
    {
        using var client = fixture.Factory.CreateClient();
        var result = await GetProductsAsync(client, "minPrice=5000");

        Assert.Equal(new[] { "Fold Phone" }, result.Items.Select(product => product.Name));
    }

    [Fact]
    public async Task Maximum_price_filter_is_inclusive()
    {
        using var client = fixture.Factory.CreateClient();
        var result = await GetProductsAsync(client, "maxPrice=100");

        Assert.Equal(new[] { "Smart Plug" }, result.Items.Select(product => product.Name));
    }

    [Fact]
    public async Task In_stock_filter_handles_both_values()
    {
        using var client = fixture.Factory.CreateClient();
        var available = await GetProductsAsync(client, "inStock=true");
        var unavailable = await GetProductsAsync(client, "inStock=false");

        Assert.Equal(24, available.TotalCount);
        Assert.Empty(unavailable.Items);
        Assert.Equal(0, unavailable.TotalCount);
    }

    [Fact]
    public async Task Filters_can_be_combined()
    {
        using var client = fixture.Factory.CreateClient();
        var categories = await client.GetFromJsonAsync<List<CategoryListDto>>("/api/categories");
        var audioId = categories!.Single(category => category.Name == "Audio").Id;
        var result = await GetProductsAsync(client,
            $"search=wireless&categoryId={audioId}&minPrice=300&maxPrice=500&inStock=true");

        Assert.Equal(new[] { "Wireless Earbuds" }, result.Items.Select(product => product.Name));
        Assert.Equal(1, result.TotalCount);
    }

    [Theory]
    [InlineData("price_asc", "Smart Plug", "Fold Phone")]
    [InlineData("price_desc", "Fold Phone", "Smart Plug")]
    [InlineData("name", "35mm Portrait Lens", "Wireless Mouse")]
    public async Task Sort_orders_products(string sort, string firstName, string lastName)
    {
        using var client = fixture.Factory.CreateClient();
        var result = await GetProductsAsync(client, $"sort={sort}&pageSize=50");

        Assert.Equal(24, result.Items.Count);
        Assert.Equal(firstName, result.Items[0].Name);
        Assert.Equal(lastName, result.Items[^1].Name);
    }

    [Fact]
    public async Task Newest_sort_is_ordered_by_creation_time()
    {
        using var client = fixture.Factory.CreateClient();
        var result = await GetProductsAsync(client, "sort=newest&pageSize=50");

        Assert.Equal(result.Items.OrderByDescending(product => product.CreatedAt).ThenByDescending(product => product.Id), result.Items);
    }

    [Fact]
    public async Task Paging_clamps_page_and_page_size_and_caps_page_size_at_50()
    {
        using var client = fixture.Factory.CreateClient();
        var firstPage = await GetProductsAsync(client, "page=0&pageSize=0");
        var secondPage = await GetProductsAsync(client, "page=2&pageSize=10");
        var oversizedPage = await GetProductsAsync(client, "pageSize=500");
        var pastEnd = await GetProductsAsync(client, "page=4&pageSize=10");

        Assert.Equal(1, firstPage.Page);
        Assert.Equal(1, firstPage.PageSize);
        Assert.Single(firstPage.Items);
        Assert.Equal(10, secondPage.Items.Count);
        Assert.Equal(3, secondPage.TotalPages);
        Assert.Equal(50, oversizedPage.PageSize);
        Assert.Equal(24, oversizedPage.Items.Count);
        Assert.Empty(pastEnd.Items);
    }

    [Fact]
    public async Task Product_details_include_specs_supplier_delivery_and_stock()
    {
        using var client = fixture.Factory.CreateClient();
        var list = await GetProductsAsync(client, "search=North%2014%20Laptop");
        using var response = await client.GetAsync($"/api/products/{list.Items.Single().Id}");
        var details = await response.Content.ReadFromJsonAsync<ProductDetailsDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(details);
        Assert.Equal("16 GB", details.Specifications["Memory"]);
        Assert.Equal("Northstar Supply", details.SupplierName);
        Assert.Equal(12, details.Stock);
        Assert.Equal(3, details.DeliveryMethods.Count);
    }

    [Fact]
    public async Task Unknown_and_inactive_products_return_not_found()
    {
        using var client = fixture.Factory.CreateClient();
        using var unknownResponse = await client.GetAsync("/api/products/2147483647");

        using var scope = fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await dbContext.Categories.FirstAsync();
        var supplier = await dbContext.Suppliers.FirstAsync();
        var inactiveProduct = new Product
        {
            Name = "Hidden test item",
            Description = "An inactive product for the not-found check.",
            Price = 1m,
            Stock = 1,
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            IsActive = false
        };
        dbContext.Products.Add(inactiveProduct);
        await dbContext.SaveChangesAsync();

        using var inactiveResponse = await client.GetAsync($"/api/products/{inactiveProduct.Id}");

        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inactiveResponse.StatusCode);
    }

    [Fact]
    public async Task By_ids_returns_active_products_in_requested_order()
    {
        using var client = fixture.Factory.CreateClient();
        var allProducts = await GetProductsAsync(client, "pageSize=50");
        var requested = allProducts.Items
            .Where(product => product.Name is "Smart Plug" or "North 14 Laptop")
            .OrderByDescending(product => product.Name)
            .ToArray();
        var ids = string.Join(',', requested.Select(product => product.Id));

        var result = await client.GetFromJsonAsync<List<ProductListDto>>($"/api/products/by-ids?ids={ids}");

        Assert.Equal(requested.Select(product => product.Id), result!.Select(product => product.Id));
    }

    private static async Task<PagedResult<ProductListDto>> GetProductsAsync(HttpClient client, string query)
    {
        var result = await client.GetFromJsonAsync<PagedResult<ProductListDto>>($"/api/products?{query}");
        return Assert.IsType<PagedResult<ProductListDto>>(result);
    }
}