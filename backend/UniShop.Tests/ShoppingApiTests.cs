using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;
using UniShop.Api.Services;
using Xunit;

namespace UniShop.Tests;

public class ShoppingApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Adding_the_same_wishlist_item_twice_is_idempotent()
    {
        using var client = await RegisterClientAsync();
        var product = await GetFirstProductAsync(client);

        using var firstAdd = await client.PutAsync($"/api/wishlist/{product.Id}", null);
        using var secondAdd = await client.PutAsync($"/api/wishlist/{product.Id}", null);
        var wishlist = await client.GetFromJsonAsync<List<WishlistItemDto>>("/api/wishlist");
        var savedItems = wishlist ?? throw new InvalidOperationException("Wishlist response was empty.");

        Assert.Equal(HttpStatusCode.OK, firstAdd.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondAdd.StatusCode);
        Assert.Single(savedItems);
        Assert.Equal(product.Id, savedItems[0].ProductId);
    }

    [Fact]
    public async Task Cart_quantity_validation_rejects_non_positive_and_over_stock_values()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 3);

        using var zeroQuantity = await client.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 0 });
        using var tooMany = await client.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 4 });
        using var accepted = await client.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, zeroQuantity.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
    }

    [Fact]
    public async Task Guest_cart_merge_sums_duplicate_ids_and_caps_at_current_stock()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 10);

        using var response = await client.PostAsJsonAsync("/api/cart/merge", new
        {
            items = new[]
            {
                new { productId = product.Id, quantity = 7 },
                new { productId = product.Id, quantity = 8 }
            }
        });
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(10, cart.Items[0].Quantity);
        Assert.Equal(product.Price * 10, cart.Items[0].LineTotal);
        Assert.False(cart.HasStockWarnings);
    }

    [Fact]
    public async Task Cart_snapshot_warns_when_stock_drops_below_saved_quantity()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 8);
        using var addResponse = await client.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 5 });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storedProduct = await db.Products.SingleAsync(item => item.Id == product.Id);
            storedProduct.Stock = 2;
            await db.SaveChangesAsync();
        }

        var cart = await client.GetFromJsonAsync<CartDto>("/api/cart");
        Assert.NotNull(cart);
        Assert.True(cart.HasStockWarnings);
        Assert.True(cart.Items.Single().ExceedsStock);
        Assert.Equal(2, cart.Items.Single().Stock);
    }

    [Fact]
    public async Task Clearing_cart_removes_all_items()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 5);
        using var addResponse = await client.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using var clearResponse = await client.DeleteAsync("/api/cart");
        var getResult = await client.GetFromJsonAsync<CartDto>("/api/cart");

        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);
        Assert.Empty(getResult!.Items);
        Assert.Equal(0m, getResult.Total);
    }

    [Fact]
    public async Task Failed_move_to_cart_keeps_wishlist_item()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 0);
        using var addResponse = await client.PutAsync($"/api/wishlist/{product.Id}", null);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using var moveResponse = await client.PostAsync($"/api/wishlist/{product.Id}/move-to-cart", null);
        var wishlist = await client.GetFromJsonAsync<List<WishlistItemDto>>("/api/wishlist");
        var cart = await client.GetFromJsonAsync<CartDto>("/api/cart");

        Assert.Equal(HttpStatusCode.Conflict, moveResponse.StatusCode);
        Assert.Single(wishlist!);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Move_to_cart_transfers_item_and_removes_it_from_wishlist()
    {
        using var client = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 4);
        using var addResponse = await client.PutAsync($"/api/wishlist/{product.Id}", null);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        using var moveResponse = await client.PostAsync($"/api/wishlist/{product.Id}/move-to-cart", null);
        var cart = await moveResponse.Content.ReadFromJsonAsync<CartDto>();
        var wishlist = await client.GetFromJsonAsync<List<WishlistItemDto>>("/api/wishlist");

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);
        Assert.NotNull(cart);
        Assert.Equal(product.Id, Assert.Single(cart.Items).ProductId);
        Assert.Equal(1, cart.Items[0].Quantity);
        Assert.Empty(wishlist!);
    }

    [Fact]
    public async Task Cart_and_wishlist_data_are_isolated_between_users()
    {
        using var firstUser = await RegisterClientAsync();
        using var secondUser = await RegisterClientAsync();
        var product = await CreateTestProductAsync(stock: 9);

        using var addWishlist = await firstUser.PutAsync($"/api/wishlist/{product.Id}", null);
        using var addCart = await firstUser.PutAsJsonAsync($"/api/cart/items/{product.Id}", new { quantity = 2 });
        Assert.Equal(HttpStatusCode.OK, addWishlist.StatusCode);
        Assert.Equal(HttpStatusCode.OK, addCart.StatusCode);

        var secondWishlist = await secondUser.GetFromJsonAsync<List<WishlistItemDto>>("/api/wishlist");
        var secondCart = await secondUser.GetFromJsonAsync<CartDto>("/api/cart");
        using var secondUserWishlistDelete = await secondUser.DeleteAsync($"/api/wishlist/{product.Id}");
        using var secondUserCartDelete = await secondUser.DeleteAsync($"/api/cart/items/{product.Id}");
        var firstWishlist = await firstUser.GetFromJsonAsync<List<WishlistItemDto>>("/api/wishlist");
        var firstCart = await firstUser.GetFromJsonAsync<CartDto>("/api/cart");

        Assert.Empty(secondWishlist!);
        Assert.Empty(secondCart!.Items);
        Assert.Equal(HttpStatusCode.NoContent, secondUserWishlistDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondUserCartDelete.StatusCode);
        Assert.Single(firstWishlist!);
        Assert.Equal(2, firstCart!.Items.Single().Quantity);
    }

    private async Task<HttpClient> RegisterClientAsync()
    {
        var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Shopping Test Client",
            email = $"shopping-{Guid.NewGuid():N}@example.test",
            password = "ShoppingPass123"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static async Task<Product> GetFirstProductAsync(HttpClient client)
    {
        var products = await client.GetFromJsonAsync<PagedResult<ProductListDto>>("/api/products?pageSize=50");
        var product = products!.Items.First();
        return new Product { Id = product.Id, Name = product.Name, Price = product.Price, Stock = product.Stock };
    }

    private async Task<Product> CreateTestProductAsync(int stock)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.FirstAsync();
        var supplier = await db.Suppliers.FirstAsync();
        var product = new Product
        {
            Name = $"Shopping Test Product {Guid.NewGuid():N}",
            Description = "Product fixture for cart and wishlist tests.",
            Price = 12.5m,
            Stock = stock,
            CategoryId = category.Id,
            SupplierId = supplier.Id
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }
}