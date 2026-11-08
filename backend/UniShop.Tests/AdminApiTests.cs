using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using UniShop.Api.Contracts;
using UniShop.Api.Services;
using Xunit;

namespace UniShop.Tests;

public class AdminApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Admin_can_create_update_read_and_delete_product()
    {
        using var client = await CreateAdminClientAsync();
        var categories = await client.GetFromJsonAsync<List<CategoryAdminDto>>("/api/admin/categories");
        var suppliers = await client.GetFromJsonAsync<List<SupplierAdminDto>>("/api/admin/suppliers");
        var deliveryMethods = await client.GetFromJsonAsync<List<DeliveryMethodAdminDto>>("/api/admin/delivery-methods");
        var categoryId = categories?.FirstOrDefault()?.Id ?? throw new InvalidOperationException("Seed category missing.");
        var supplierId = suppliers?.FirstOrDefault()?.Id ?? throw new InvalidOperationException("Seed supplier missing.");
        var selectedDeliveryMethods = deliveryMethods?.Take(2).Select(method => method.Id).ToArray()
            ?? throw new InvalidOperationException("Seed delivery methods missing.");
        var request = new
        {
            name = "Admin CRUD Test Product",
            description = "A product created by the admin integration test.",
            specifications = new[] { new { key = "Color", value = "Green" } },
            price = 42.50m,
            stock = 7,
            imageUrl = "https://example.test/product.png",
            isActive = true,
            categoryId,
            supplierId,
            deliveryMethodIds = selectedDeliveryMethods
        };

        using var createResponse = await client.PostAsJsonAsync("/api/admin/products", request);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProductDto>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Green", created.Specifications.Single().Value);
        Assert.Equal(2, created.DeliveryMethodIds.Count);

        using var updateResponse = await client.PutAsJsonAsync($"/api/admin/products/{created.Id}", new
        {
            name = "Admin CRUD Test Product Updated",
            description = "Updated product description.",
            specifications = new[] { new { key = "Color", value = "Coral" }, new { key = "Size", value = "M" } },
            price = 50m,
            stock = 0,
            imageUrl = "https://example.test/updated-product.png",
            isActive = true,
            categoryId,
            supplierId,
            deliveryMethodIds = new[] { selectedDeliveryMethods[0] }
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<AdminProductDto>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Admin CRUD Test Product Updated", updated.Name);
        Assert.Equal(0, updated.Stock);
        Assert.Equal(2, updated.Specifications.Count);
        Assert.Single(updated.DeliveryMethodIds);

        var adminList = await client.GetFromJsonAsync<PagedResult<AdminProductListDto>>("/api/admin/products?search=CRUD%20Test&pageSize=50");
        Assert.Contains(adminList!.Items, product => product.Id == created.Id && product.IsActive);

        using var deleteResponse = await client.DeleteAsync($"/api/admin/products/{created.Id}");
        using var getDeletedResponse = await client.GetAsync($"/api/admin/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_access_admin_endpoints()
    {
        using var client = fixture.Factory.CreateClient();
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Regular Client",
            email = $"client-{Guid.NewGuid():N}@example.test",
            password = "ClientPass123"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        using var response = await client.GetAsync("/api/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Product_order_reference_blocks_delete_and_admin_can_deactivate_and_edit_stock()
    {
        using var admin = await CreateAdminClientAsync();
        using var buyer = fixture.Factory.CreateClient();
        using var registerResponse = await buyer.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Order Product Buyer",
            email = $"product-order-{Guid.NewGuid():N}@example.test",
            password = "ClientPass123"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        buyer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var categories = await admin.GetFromJsonAsync<List<CategoryAdminDto>>("/api/admin/categories");
        var suppliers = await admin.GetFromJsonAsync<List<SupplierAdminDto>>("/api/admin/suppliers");
        var methods = await admin.GetFromJsonAsync<List<DeliveryMethodAdminDto>>("/api/admin/delivery-methods");
        using var createResponse = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = $"Order Referenced {Guid.NewGuid():N}",
            description = "Product referenced by an order.",
            specifications = Array.Empty<object>(),
            price = 25m,
            stock = 4,
            imageUrl = (string?)null,
            isActive = true,
            categoryId = categories!.First().Id,
            supplierId = suppliers!.First().Id,
            deliveryMethodIds = methods!.Select(method => method.Id).ToArray()
        });
        var product = await createResponse.Content.ReadFromJsonAsync<AdminProductDto>();
        using var addressResponse = await buyer.PostAsJsonAsync("/api/addresses", new
        {
            label = "Home",
            fullName = "Product Buyer",
            phone = "+40 721 000 111",
            street = "1 Test Street",
            city = "Bucharest",
            county = "Bucharest",
            postalCode = "123456",
            country = "Romania",
            isDefault = true
        });
        var address = await addressResponse.Content.ReadFromJsonAsync<AddressDto>();
        using var cartResponse = await buyer.PutAsJsonAsync($"/api/cart/items/{product!.Id}", new { quantity = 1 });
        var options = await buyer.GetFromJsonAsync<List<DeliveryOptionDto>>("/api/checkout/delivery-options");
        using var checkout = await buyer.PostAsJsonAsync("/api/orders", new
        {
            addressId = address!.Id,
            contact = new { fullName = "Product Buyer", email = auth.User.Email, phone = "+40 721 000 111" },
            deliveryMethodId = options!.First().Id,
            paymentMethod = "card",
            notes = (string?)null
        });

        using var deleteResponse = await admin.DeleteAsync($"/api/admin/products/{product.Id}");
        var problem = await deleteResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        using var stockResponse = await admin.PatchAsJsonAsync($"/api/admin/products/{product.Id}/stock", new { stock = 12 });
        using var deactivateResponse = await admin.PatchAsJsonAsync($"/api/admin/products/{product.Id}/active", new { });
        var updatedProduct = await admin.GetFromJsonAsync<AdminProductDto>($"/api/admin/products/{product.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, addressResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Contains("Deactivate", problem!.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NoContent, stockResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
        Assert.Equal(12, updatedProduct!.Stock);
        Assert.False(updatedProduct.IsActive);
    }

    [Fact]
    public async Task Client_cannot_access_admin_order_routes_or_change_product_stock()
    {
        using var client = fixture.Factory.CreateClient();
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Regular Client",
            email = $"admin-route-client-{Guid.NewGuid():N}@example.test",
            password = "ClientPass123"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        using var ordersResponse = await client.GetAsync("/api/admin/orders");
        using var stockResponse = await client.PatchAsJsonAsync("/api/admin/products/1/stock", new { stock = 9 });

        Assert.Equal(HttpStatusCode.Forbidden, ordersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, stockResponse.StatusCode);
    }

    [Fact]
    public async Task Product_validation_rejects_bad_price_references_and_duplicate_specification_keys()
    {
        using var client = await CreateAdminClientAsync();
        var deliveryMethods = await client.GetFromJsonAsync<List<DeliveryMethodAdminDto>>("/api/admin/delivery-methods");
        var deliveryMethod = deliveryMethods?.FirstOrDefault() ?? throw new InvalidOperationException("Seed delivery method missing.");
        var request = new
        {
            name = "Invalid Product",
            description = "Invalid fields should produce a useful validation problem.",
            specifications = new[]
            {
                new { key = "Color", value = "Green" },
                new { key = " color ", value = "Coral" }
            },
            price = 0m,
            stock = -1,
            imageUrl = "not-a-url",
            isActive = true,
            categoryId = int.MaxValue,
            supplierId = int.MaxValue,
            deliveryMethodIds = new[] { deliveryMethod.Id }
        };

        using var response = await client.PostAsJsonAsync("/api/admin/products", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("Price", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Stock", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("ImageUrl", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Specifications", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Product_validation_rejects_unknown_category_and_supplier()
    {
        using var client = await CreateAdminClientAsync();
        var deliveryMethods = await client.GetFromJsonAsync<List<DeliveryMethodAdminDto>>("/api/admin/delivery-methods");
        var request = new
        {
            name = "Invalid References Product",
            description = "The IDs do not exist.",
            specifications = new[] { new { key = "Color", value = "Green" } },
            price = 10m,
            stock = 1,
            imageUrl = (string?)null,
            isActive = true,
            categoryId = int.MaxValue,
            supplierId = int.MaxValue,
            deliveryMethodIds = new[] { deliveryMethods!.First().Id }
        };

        using var response = await client.PostAsJsonAsync("/api/admin/products", request);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CategoryId", problem!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("SupplierId", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Category_in_use_returns_conflict_and_clear_message()
    {
        using var client = await CreateAdminClientAsync();
        var categories = await client.GetFromJsonAsync<List<CategoryAdminDto>>("/api/admin/categories");
        var category = categories!.First(item => item.ProductCount > 0);

        using var response = await client.DeleteAsync($"/api/admin/categories/{category.Id}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("products", problem!.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_create_update_and_delete_reference_data()
    {
        using var client = await CreateAdminClientAsync();

        using var categoryCreate = await client.PostAsJsonAsync("/api/admin/categories", new
        {
            name = "Admin Test Category",
            description = "Temporary category"
        });
        var category = await categoryCreate.Content.ReadFromJsonAsync<CategoryAdminDto>();
        Assert.Equal(HttpStatusCode.Created, categoryCreate.StatusCode);

        using var categoryUpdate = await client.PutAsJsonAsync($"/api/admin/categories/{category!.Id}", new
        {
            name = "Admin Test Category Updated",
            description = "Updated category"
        });
        Assert.Equal(HttpStatusCode.OK, categoryUpdate.StatusCode);

        using var supplierCreate = await client.PostAsJsonAsync("/api/admin/suppliers", new
        {
            name = "Admin Test Supplier",
            contactEmail = "admin-supplier@example.test",
            phone = "+1-555-0110"
        });
        var supplier = await supplierCreate.Content.ReadFromJsonAsync<SupplierAdminDto>();
        Assert.Equal(HttpStatusCode.Created, supplierCreate.StatusCode);

        using var supplierUpdate = await client.PutAsJsonAsync($"/api/admin/suppliers/{supplier!.Id}", new
        {
            name = "Admin Test Supplier Updated",
            contactEmail = "admin-supplier-updated@example.test",
            phone = "+1-555-0111"
        });
        Assert.Equal(HttpStatusCode.OK, supplierUpdate.StatusCode);

        using var methodCreate = await client.PostAsJsonAsync("/api/admin/delivery-methods", new
        {
            name = "Admin Test Delivery",
            price = 12.5m,
            estimatedDays = 2
        });
        var method = await methodCreate.Content.ReadFromJsonAsync<DeliveryMethodAdminDto>();
        Assert.Equal(HttpStatusCode.Created, methodCreate.StatusCode);

        using var methodUpdate = await client.PutAsJsonAsync($"/api/admin/delivery-methods/{method!.Id}", new
        {
            name = "Admin Test Delivery Updated",
            price = 15m,
            estimatedDays = 1
        });
        Assert.Equal(HttpStatusCode.OK, methodUpdate.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/categories/{category.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/suppliers/{supplier.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/delivery-methods/{method.Id}")).StatusCode);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = fixture.Factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@unishop.test",
            password = "AdminPass123!"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }
}