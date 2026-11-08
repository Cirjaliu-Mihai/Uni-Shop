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

public class AdminManagementApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task Admin_can_search_lock_unlock_and_change_roles_but_cannot_lock_or_demote_self_or_last_admin()
    {
        using var admin = await CreateAdminClientAsync();
        var client = await RegisterClientAsync();

        var users = await admin.Client.GetFromJsonAsync<PagedResult<AdminUserListDto>>(
            $"/api/admin/users?search={Uri.EscapeDataString(client.Email)}&page=1&pageSize=10");
        var listedUser = Assert.Single(users!.Items);
        Assert.Equal(client.Id, listedUser.Id);
        Assert.Contains("Client", listedUser.Roles);
        Assert.Equal(0, listedUser.OrderCount);
        Assert.False(listedUser.IsLocked);

        using var selfLock = await admin.Client.PatchAsync($"/api/admin/users/{admin.UserId}/lock", null);
        using var selfDemote = await admin.Client.PatchAsJsonAsync($"/api/admin/users/{admin.UserId}/role", new { role = "Client" });
        Assert.Equal(HttpStatusCode.Conflict, selfLock.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, selfDemote.StatusCode);

        using var promote = await admin.Client.PatchAsJsonAsync($"/api/admin/users/{client.Id}/role", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        using var secondaryAdmin = await LoginAsync(client.Email, client.Password);

        using var lockUser = await admin.Client.PatchAsync($"/api/admin/users/{client.Id}/lock", null);
        var locked = await lockUser.Content.ReadFromJsonAsync<AdminUserListDto>();
        using var unlockUser = await admin.Client.PatchAsync($"/api/admin/users/{client.Id}/unlock", null);
        var unlocked = await unlockUser.Content.ReadFromJsonAsync<AdminUserListDto>();
        Assert.Equal(HttpStatusCode.OK, lockUser.StatusCode);
        Assert.True(locked!.IsLocked);
        Assert.Equal(HttpStatusCode.OK, unlockUser.StatusCode);
        Assert.False(unlocked!.IsLocked);

        using var demoteSecond = await admin.Client.PatchAsJsonAsync($"/api/admin/users/{client.Id}/role", new { role = "Client" });
        using var demoteLastAdmin = await secondaryAdmin.Client.PatchAsJsonAsync($"/api/admin/users/{admin.UserId}/role", new { role = "Client" });
        Assert.Equal(HttpStatusCode.OK, demoteSecond.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, demoteLastAdmin.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_access_admin_users_or_reports()
    {
        var client = await RegisterClientAsync();
        using var response = await client.Client.GetAsync("/api/admin/users");
        using var report = await client.Client.GetAsync("/api/admin/reports/sales?from=2026-01-01T00%3A00%3A00Z&to=2026-01-31T23%3A59%3A59Z&groupBy=day");
        client.Client.Dispose();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, report.StatusCode);
    }

    [Fact]
    public async Task Reports_sum_known_orders_and_exclude_cancelled_orders()
    {
        using var admin = await CreateAdminClientAsync();
        using var buyer = await RegisterClientAsync();
        var productId = await CreateProductAsync(price: 25m, stock: 20);
        var first = await CreateOrderAsync(buyer.Client, productId, 2);
        var second = await CreateOrderAsync(buyer.Client, productId, 1);
        var cancelled = await CreateOrderAsync(buyer.Client, productId, 3);
        await SetOrderDateAsync(first.Id, new DateTimeOffset(2026, 2, 2, 10, 0, 0, TimeSpan.Zero));
        await SetOrderDateAsync(second.Id, new DateTimeOffset(2026, 2, 3, 10, 0, 0, TimeSpan.Zero));
        await SetOrderDateAsync(cancelled.Id, new DateTimeOffset(2026, 2, 4, 10, 0, 0, TimeSpan.Zero));

        using var cancelResponse = await admin.Client.PatchAsJsonAsync(
            $"/api/admin/orders/{cancelled.Id}/status",
            new { status = "cancelled" });
        var from = Uri.EscapeDataString("2026-02-01T00:00:00+00:00");
        var to = Uri.EscapeDataString("2026-02-28T23:59:59+00:00");
        var sales = await admin.Client.GetFromJsonAsync<List<SalesReportPointDto>>(
            $"/api/admin/reports/sales?from={from}&to={to}&groupBy=month");
        var popular = await admin.Client.GetFromJsonAsync<List<PopularProductReportDto>>(
            $"/api/admin/reports/popular-products?from={from}&to={to}&top=1");

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var month = Assert.Single(sales!);
        Assert.Equal(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), month.PeriodStart);
        Assert.Equal(2, month.OrderCount);
        Assert.Equal(75m, month.Revenue);
        var bestSeller = Assert.Single(popular!);
        Assert.Equal(productId, bestSeller.ProductId);
        Assert.Equal(3, bestSeller.QuantitySold);
        Assert.Equal(75m, bestSeller.Revenue);
    }

    private async Task<TestAdminClient> CreateAdminClientAsync() =>
        await LoginAsync("admin@unishop.test", "AdminPass123!");

    private async Task<TestAdminClient> LoginAsync(string email, string password)
    {
        var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return new TestAdminClient(client, auth.User.Id);
    }

    private async Task<TestClient> RegisterClientAsync()
    {
        var client = fixture.Factory.CreateClient();
        var email = $"admin-management-{Guid.NewGuid():N}@example.test";
        const string password = "ManagementPass123";
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Management Test Client",
            email,
            password
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return new TestClient(client, auth.User.Id, email, password);
    }

    private async Task<int> CreateProductAsync(decimal price, int stock)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var product = new Product
        {
            Name = $"Report Test Product {Guid.NewGuid():N}",
            Description = "Product fixture for report tests.",
            Price = price,
            Stock = stock,
            CategoryId = await db.Categories.Select(category => category.Id).FirstAsync(),
            SupplierId = await db.Suppliers.Select(supplier => supplier.Id).FirstAsync(),
            DeliveryMethods = await db.DeliveryMethods.ToListAsync()
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product.Id;
    }

    private static async Task<OrderDto> CreateOrderAsync(HttpClient client, int productId, int quantity)
    {
        using var addressResponse = await client.PostAsJsonAsync("/api/addresses", new
        {
            label = "Report Test",
            fullName = "Report Buyer",
            phone = "+40 721 000 111",
            street = "1 Test Street",
            city = "Bucharest",
            county = "Bucharest",
            postalCode = "123456",
            country = "Romania",
            isDefault = true
        });
        var address = await addressResponse.Content.ReadFromJsonAsync<AddressDto>();
        using var cartResponse = await client.PutAsJsonAsync($"/api/cart/items/{productId}", new { quantity });
        var deliveryOptions = await client.GetFromJsonAsync<List<DeliveryOptionDto>>("/api/checkout/delivery-options");
        using var checkout = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId = address!.Id,
            contact = new { fullName = "Report Buyer", email = "report@example.test", phone = "+40 721 000 111" },
            deliveryMethodId = deliveryOptions!.First().Id,
            paymentMethod = "cashOnDelivery",
            notes = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, addressResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        return (await checkout.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions))!;
    }

    private async Task SetOrderDateAsync(int orderId, DateTimeOffset createdAt)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Orders.Where(order => order.Id == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.CreatedAt, createdAt));
    }

    private sealed record TestAdminClient(HttpClient Client, string UserId) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }

    private sealed record TestClient(HttpClient Client, string Id, string Email, string Password) : IDisposable
    {
        public void Dispose() => Client.Dispose();
    }
}