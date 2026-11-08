using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniShop.Api.Contracts;
using UniShop.Api.Domain;
using UniShop.Api.Persistence;
using UniShop.Api.Services;
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
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
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

    [Fact]
    public async Task Customer_order_history_is_private_and_cancellation_refunds_and_restocks_once()
    {
        using var buyer = await CreateClientAsync();
        using var otherBuyer = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 5, price: 21m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 2, paymentMethod: "card");
        var foreignOrder = await CreateOrderAsync(otherBuyer.Client, productId, quantity: 1, paymentMethod: "card");
        await MarkOrderPaidAsync(order.Id);

        var history = await buyer.Client.GetFromJsonAsync<PagedResult<OrderListDto>>("/api/orders?page=1&pageSize=50", ResponseJsonOptions);
        using var foreignDetail = await buyer.Client.GetAsync($"/api/orders/{foreignOrder.Id}");
        using var cancelResponse = await buyer.Client.PostAsync($"/api/orders/{order.Id}/cancel", null);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions);
        using var duplicateCancel = await buyer.Client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Contains(history!.Items, item => item.Id == order.Id);
        Assert.DoesNotContain(history.Items, item => item.Id == foreignOrder.Id);
        Assert.Equal(HttpStatusCode.NotFound, foreignDetail.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
        Assert.Equal(PaymentStatus.Refunded, cancelled.PaymentStatus);
        Assert.Equal(HttpStatusCode.Conflict, duplicateCancel.StatusCode);
        Assert.Equal(4, await GetStockAsync(productId));
    }

    [Fact]
    public async Task Admin_transitions_are_enforced_and_cancellation_restocks()
    {
        using var buyer = await CreateClientAsync();
        using var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(stock: 5, price: 16m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 2, paymentMethod: "card");
        await MarkOrderPaidAsync(order.Id);

        using var shippedResponse = await admin.PatchAsJsonAsync($"/api/admin/orders/{order.Id}/status", new { status = "shipped" });
        var shipped = await shippedResponse.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions);
        using var deliveredResponse = await admin.PatchAsJsonAsync($"/api/admin/orders/{order.Id}/status", new { status = "delivered" });
        using var invalidResponse = await admin.PatchAsJsonAsync($"/api/admin/orders/{order.Id}/status", new { status = "cancelled" });
        var otherOrder = await CreateOrderAsync(buyer.Client, productId, quantity: 1, paymentMethod: "card");
        using var cancelledResponse = await admin.PatchAsJsonAsync($"/api/admin/orders/{otherOrder.Id}/status", new { status = "cancelled" });

        Assert.Equal(HttpStatusCode.OK, shippedResponse.StatusCode);
        Assert.Equal(OrderStatus.Shipped, shipped!.Status);
        Assert.Equal(HttpStatusCode.OK, deliveredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, invalidResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelledResponse.StatusCode);
        Assert.Equal(3, await GetStockAsync(productId));
    }

    [Fact]
    public async Task Delivered_cash_on_delivery_order_becomes_paid()
    {
        using var buyer = await CreateClientAsync();
        using var admin = await CreateAdminClientAsync();
        var productId = await CreateProductAsync(stock: 3, price: 19m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 1, paymentMethod: "cashOnDelivery");
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);

        using var shipped = await admin.PatchAsJsonAsync($"/api/admin/orders/{order.Id}/status", new { status = "shipped" });
        using var delivered = await admin.PatchAsJsonAsync($"/api/admin/orders/{order.Id}/status", new { status = "delivered" });
        var updated = await delivered.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions);

        Assert.Equal(HttpStatusCode.OK, shipped.StatusCode);
        Assert.Equal(HttpStatusCode.OK, delivered.StatusCode);
        Assert.Equal(PaymentStatus.Paid, updated!.PaymentStatus);
    }

    [Fact]
    public async Task Signed_checkout_completed_webhook_marks_card_order_paid()
    {
        using var buyer = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 3, price: 24m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 1, paymentMethod: "card");
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        const string sessionId = "cs_test_signed_checkout";
        await SetPaymentReferenceAsync(order.Id, sessionId);

        var amountMinor = (long)(order.Total * 100m);
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = "evt_checkout_test",
            ["object"] = "event",
            ["api_version"] = "2025-03-31.basil",
            ["created"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["data"] = new Dictionary<string, object?>
            {
                ["object"] = new Dictionary<string, object?>
                {
                    ["id"] = sessionId,
                    ["object"] = "checkout.session",
                    ["client_reference_id"] = order.Id.ToString(),
                    ["payment_intent"] = "pi_test_checkout",
                    ["payment_status"] = "paid",
                    ["amount_total"] = amountMinor,
                    ["currency"] = "ron",
                    ["metadata"] = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() }
                }
            },
            ["livemode"] = false,
            ["pending_webhooks"] = 1,
            ["type"] = "checkout.session.completed"
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(
            "Stripe-Signature",
            Stripe.EventUtility.GenerateSignatureHeader(payload, "whsec_test_webhook_secret_for_unishop"));

        using var webhookClient = fixture.Factory.CreateClient();
        using var response = await webhookClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        var paidOrder = await buyer.Client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", ResponseJsonOptions);

        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        Assert.Equal(PaymentStatus.Paid, paidOrder!.PaymentStatus);
    }

    [Fact]
    public async Task Invalid_stripe_webhook_signature_is_rejected()
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", "t=1,v1=invalid");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_stripe_configuration_returns_a_shopper_safe_message()
    {
        using var buyer = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 2, price: 18m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 1, paymentMethod: "card");

        using var response = await buyer.Client.PostAsJsonAsync($"/api/payments/{order.Id}/checkout-session", new { });
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Card payments are temporarily unavailable. Please choose cash on delivery or try again later.", problem!.Detail);
        Assert.DoesNotContain("Stripe:SecretKey", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Expiring_pending_card_order_marks_payment_failed_and_restocks()
    {
        using var buyer = await CreateClientAsync();
        var productId = await CreateProductAsync(stock: 3, price: 29m);
        var order = await CreateOrderAsync(buyer.Client, productId, quantity: 1, paymentMethod: "card");
        await SetPaymentCreatedAtAsync(order.Id, DateTimeOffset.UtcNow.AddMinutes(-31));

        using var scope = fixture.Factory.Services.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
        var expiredCount = await paymentService.ExpirePendingOrdersAsync(DateTimeOffset.UtcNow.AddMinutes(-30), CancellationToken.None);
        var updatedOrder = await buyer.Client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.Id}", ResponseJsonOptions);

        Assert.Equal(1, expiredCount);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder!.Status);
        Assert.Equal(PaymentStatus.Failed, updatedOrder.PaymentStatus);
        Assert.Equal(3, await GetStockAsync(productId));
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

    private async Task<OrderDto> CreateOrderAsync(HttpClient client, int productId, int quantity, string paymentMethod)
    {
        var address = await CreateAddressAsync(client);
        var deliveryMethodId = await AddCartItemAndGetDeliveryMethodAsync(client, productId, quantity);
        using var response = await client.PostAsJsonAsync("/api/orders", new
        {
            addressId = address.Id,
            contact = new { fullName = "Order Integration Test", email = "order@example.test", phone = "+40 721 000 111" },
            deliveryMethodId,
            paymentMethod,
            notes = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDto>(ResponseJsonOptions))!;
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

    private async Task MarkOrderPaidAsync(int orderId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Payments.Where(payment => payment.OrderId == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.Status, PaymentStatus.Paid));
    }

    private async Task SetPaymentReferenceAsync(int orderId, string reference)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Payments.Where(payment => payment.OrderId == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.ProviderReference, reference));
    }

    private async Task SetPaymentCreatedAtAsync(int orderId, DateTimeOffset createdAt)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Payments.Where(payment => payment.OrderId == orderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(payment => payment.CreatedAt, createdAt));
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