using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;
using Xunit;

namespace UniShop.Tests;

public class OrderApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task Successful_checkout_decrements_stock_and_clears_cart()
    {
        using var user = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 5, price: 20m);
        var address = await CreateAddressAsync(user.Client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(user.Client, productId, quantity: 2);

        using var response = await user.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(address.Id, deliveryMethodId));
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions);
        var cart = await user.Client.GetFromJsonAsync<CartDto>("/api/cart");
        var stock = await GetStockAsync(productId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(40m, order.Subtotal);
        Assert.Equal(2, order.Items.Single().Quantity);
        Assert.Equal(20m, order.Items.Single().UnitPrice);
        Assert.Equal(3, stock);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Insufficient_stock_rolls_back_order_stock_and_cart_changes()
    {
        using var user = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 4, price: 15m);
        var address = await CreateAddressAsync(user.Client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(user.Client, productId, quantity: 4);
        await SetStockAsync(productId, 2);

        using var response = await user.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(address.Id, deliveryMethodId));
        var cart = await user.Client.GetFromJsonAsync<CartDto>("/api/cart");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, await GetStockAsync(productId));
        Assert.Equal(4, Assert.Single(cart!.Items).Quantity);
        Assert.Equal(0, await GetOrderCountAsync(user.UserId));
    }

    [Fact]
    public async Task Parallel_checkouts_for_last_unit_only_allow_one_order()
    {
        using var firstUser = await CreateClientAsync();
        using var secondUser = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 1, price: 30m);
        var firstAddress = await CreateAddressAsync(firstUser.Client);
        var secondAddress = await CreateAddressAsync(secondUser.Client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(firstUser.Client, productId, quantity: 1);
        await AddCartItemAsync(secondUser.Client, productId, quantity: 1);

        var firstCheckout = firstUser.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(firstAddress.Id, deliveryMethodId));
        var secondCheckout = secondUser.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(secondAddress.Id, deliveryMethodId));
        using var firstResponse = await firstCheckout;
        using var secondResponse = await secondCheckout;

        var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
        Assert.Single(statuses, status => status == HttpStatusCode.Created);
        Assert.Single(statuses, status => status == HttpStatusCode.Conflict);
        Assert.Equal(0, await GetStockAsync(productId));
    }

    [Fact]
    public async Task Checkout_uses_current_server_price_and_keeps_order_price_snapshot()
    {
        using var user = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 5, price: 10m);
        var address = await CreateAddressAsync(user.Client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(user.Client, productId, quantity: 2);
        await SetPriceAsync(productId, 40m);

        using var response = await user.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(address.Id, deliveryMethodId));
        var created = await response.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(40m, created.Items.Single().UnitPrice);
        Assert.Equal(80m, created.Subtotal);

        await SetPriceAsync(productId, 80m);
        var order = await user.Client.GetFromJsonAsync<OrderDto>($"/api/orders/{created.Id}", ResponseJsonOptions);

        Assert.NotNull(order);
        Assert.Equal(40m, order.Items.Single().UnitPrice);
        Assert.Equal(80m, order.Subtotal);
    }

    [Fact]
    public async Task Another_users_address_is_rejected_without_clearing_cart()
    {
        using var buyer = await CreateClientAsync();
        using var owner = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 4, price: 15m);
        var foreignAddress = await CreateAddressAsync(owner.Client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(buyer.Client, productId, quantity: 1);

        using var response = await buyer.Client.PostAsJsonAsync("/api/orders", NewOrderRequest(foreignAddress.Id, deliveryMethodId));
        var cart = await buyer.Client.GetFromJsonAsync<CartDto>("/api/cart");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(cart!.Items);
        Assert.Equal(4, await GetStockAsync(productId));
    }

    [Fact]
    public async Task Delivery_options_are_the_intersection_for_all_cart_products()
    {
        using var user = await CreateClientAsync();
        var firstProductId = await CreateProductAsync(stock: 4, price: 15m);
        var secondProductId = await CreateProductAsync(stock: 4, price: 18m);
        await AddCartItemAsync(user.Client, firstProductId, quantity: 1);
        await AddCartItemAsync(user.Client, secondProductId, quantity: 1);
        var methodIds = await RemoveOneDeliveryMethodAsync(secondProductId);

        var options = await user.Client.GetFromJsonAsync<List<DeliveryOptionDto>>("/api/checkout/delivery-options");

        Assert.Equal(2, methodIds.Count);
        Assert.Equal(methodIds.Order(), options!.Select(option => option.Id).Order());
    }

    private async Task<TestClient> CreateClientAsync()
    {
        var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Order Test Client",
            email = $"order-{Guid.NewGuid():N}@example.test",
            password = "OrderPass123"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return new TestClient(client, auth.User.Id);
    }

    private static async Task<AddressDto> CreateAddressAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/addresses", new
        {
            label = "Home",
            fullName = "Order Test Client",
            phone = "+40 721 000 111",
            street = "1 Test Street",
            city = "Bucharest",
            county = "Bucharest",
            postalCode = "123456",
            country = "Romania",
            isDefault = true
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AddressDto>())!;
    }

    private async Task<int> CreateProductAsync(int stock, decimal price)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.FirstAsync();
        var supplier = await db.Suppliers.FirstAsync();
        var deliveryMethods = await db.DeliveryMethods.ToListAsync();
        var product = new Product
        {
            Name = $"Order Test Product {Guid.NewGuid():N}",
            Description = "Product fixture for order tests.",
            Price = price,
            Stock = stock,
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            DeliveryMethods = deliveryMethods
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    private static async Task AddCartItemAsync(HttpClient client, int productId, int quantity)
    {
        using var response = await client.PutAsJsonAsync($"/api/cart/items/{productId}", new { quantity });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<int> AddCartItemAndGetDeliveryMethodAsync(HttpClient client, int productId, int quantity)
    {
        await AddCartItemAsync(client, productId, quantity);
        var options = await client.GetFromJsonAsync<List<DeliveryOptionDto>>("/api/checkout/delivery-options");
        return options!.First().Id;
    }

    private async Task<List<int>> RemoveOneDeliveryMethodAsync(int productId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = await db.Products.Include(item => item.DeliveryMethods).SingleAsync(item => item.Id == productId);
        product.DeliveryMethods.Remove(product.DeliveryMethods.First());
        await db.SaveChangesAsync();
        return product.DeliveryMethods.Select(method => method.Id).Order().ToList();
    }

    private async Task<int> GetStockAsync(int productId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Products.Where(product => product.Id == productId).Select(product => product.Stock).SingleAsync();
    }

    private async Task SetStockAsync(int productId, int stock)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Products.Where(product => product.Id == productId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, stock));
    }

    private async Task SetPriceAsync(int productId, decimal price)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Products.Where(product => product.Id == productId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Price, price));
    }

    private static object NewOrderRequest(int addressId, int deliveryMethodId) => new
    {
        addressId,
        contact = new
        {
            fullName = "Checkout Contact",
            email = "contact@example.test",
            phone = "+40 721 000 111"
        },
        deliveryMethodId,
        paymentMethod = "card",
        notes = "Leave at the door"
    };

    private async Task<int> GetOrderCountAsync(string userId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.CountAsync(order => order.UserId == userId);
    }

    private sealed record TestClient(HttpClient Client, string UserId) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}