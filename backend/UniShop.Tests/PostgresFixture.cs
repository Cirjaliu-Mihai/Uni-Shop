using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace UniShop.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public UniShopWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Factory = new UniShopWebApplicationFactory(_postgres.GetConnectionString());
        using var client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}

public sealed class UniShopWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Admin:Email"] = "admin@unishop.test",
                ["Admin:Password"] = "AdminPass123!",
                ["JwtSettings:Key"] = "TestOnlySigningKey0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                ["JwtSettings:Issuer"] = "UniShop.Tests",
                ["JwtSettings:Audience"] = "UniShop.Tests.Client",
                ["JwtSettings:ExpiresInMinutes"] = "15",
                ["Stripe:SecretKey"] = "",
                ["Stripe:WebhookSecret"] = "whsec_test_webhook_secret_for_unishop"
            }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
    }
}