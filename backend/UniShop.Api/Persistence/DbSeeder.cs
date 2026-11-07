using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence;

public static class DbSeeder
{
    private static readonly string[] Roles = ["Admin", "Client"];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(result, $"Could not create role '{role}'.");
            }
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin user was not seeded. Configure Admin:Email and Admin:Password using user-secrets or environment variables.");
        }
        else
        {
            var admin = await userManager.FindByEmailAsync(email);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "UniShop Administrator"
                };
                var result = await userManager.CreateAsync(admin, password);
                EnsureSucceeded(result, "Could not create the configured admin user.");
            }

            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                var result = await userManager.AddToRoleAsync(admin, "Admin");
                EnsureSucceeded(result, "Could not assign the Admin role to the configured admin user.");
            }
        }

        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var categories = new[]
        {
            new Category { Name = "Computers", Description = "Laptops and desktop computers" },
            new Category { Name = "Phones", Description = "Smartphones and mobile devices" },
            new Category { Name = "Audio", Description = "Headphones and speakers" },
            new Category { Name = "Accessories", Description = "Everyday technology accessories" },
            new Category { Name = "Home Tech", Description = "Smart devices for the home" }
        };
        var suppliers = new[]
        {
            new Supplier { Name = "Northstar Supply", ContactEmail = "sales@northstar.example", Phone = "+1-555-0101" },
            new Supplier { Name = "Circuit House", ContactEmail = "orders@circuithouse.example", Phone = "+1-555-0102" },
            new Supplier { Name = "Brightline Distribution", ContactEmail = "hello@brightline.example", Phone = "+1-555-0103" }
        };
        var deliveryMethods = new[]
        {
            new DeliveryMethod { Name = "Courier", Price = 15m, EstimatedDays = 3 },
            new DeliveryMethod { Name = "Express", Price = 30m, EstimatedDays = 1 },
            new DeliveryMethod { Name = "Pickup", Price = 0m, EstimatedDays = 1 }
        };

        await db.Categories.AddRangeAsync(categories);
        await db.Suppliers.AddRangeAsync(suppliers);
        await db.DeliveryMethods.AddRangeAsync(deliveryMethods);
        await db.SaveChangesAsync();

        var products = new[]
        {
            Product("North 14 Laptop", "A portable laptop for work and study.", 3299m, 12, 0, 0, ("Memory", "16 GB"), ("Storage", "512 GB SSD")),
            Product("North 16 Laptop", "A larger laptop with a vivid display.", 4599m, 8, 0, 1, ("Memory", "32 GB"), ("Display", "16 inch")),
            Product("Compact Desktop", "A compact desktop for a clean workspace.", 2799m, 7, 0, 2, ("Memory", "16 GB"), ("Storage", "1 TB SSD")),
            Product("Everyday Phone", "A reliable smartphone for everyday use.", 1899m, 20, 1, 0, ("Storage", "128 GB"), ("Display", "6.1 inch")),
            Product("Pro Phone", "A high-performance smartphone with a premium camera.", 3899m, 9, 1, 1, ("Storage", "256 GB"), ("Camera", "50 MP")),
            Product("Fold Phone", "A foldable smartphone with a large inner display.", 5499m, 4, 1, 2, ("Storage", "512 GB"), ("Display", "7.6 inch")),
            Product("Studio Headphones", "Over-ear headphones for focused listening.", 649m, 25, 2, 0, ("Connection", "Bluetooth"), ("Battery", "40 hours")),
            Product("Wireless Earbuds", "Compact earbuds with a charging case.", 399m, 30, 2, 1, ("Connection", "Bluetooth"), ("Battery", "24 hours")),
            Product("Desk Speaker", "A compact speaker for desktop audio.", 529m, 14, 2, 2, ("Power", "30 W"), ("Connection", "Bluetooth")),
            Product("USB-C Hub", "A multiport hub for modern laptops.", 219m, 40, 3, 0, ("Ports", "7"), ("Connection", "USB-C")),
            Product("Wireless Mouse", "An ergonomic wireless mouse.", 149m, 35, 3, 1, ("DPI", "2400"), ("Connection", "2.4 GHz")),
            Product("Mechanical Keyboard", "A compact mechanical keyboard.", 449m, 18, 3, 2, ("Layout", "75%"), ("Switches", "Tactile")),
            Product("Smart Bulb Set", "A pair of app-controlled smart bulbs.", 129m, 50, 4, 0, ("Quantity", "2"), ("Connection", "Wi-Fi")),
            Product("Indoor Camera", "A compact camera for home monitoring.", 299m, 16, 4, 1, ("Resolution", "2K"), ("Connection", "Wi-Fi")),
            Product("Smart Plug", "A remotely controlled outlet with energy monitoring.", 99m, 45, 4, 2, ("Maximum load", "16 A"), ("Connection", "Wi-Fi"))
        };

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();

        Product Product(string name, string description, decimal price, int stock, int categoryIndex, int supplierIndex, params (string Key, string Value)[] specifications)
        {
            var product = new Product
            {
                Name = name,
                Description = description,
                Price = price,
                Stock = stock,
                Category = categories[categoryIndex],
                Supplier = suppliers[supplierIndex],
                Specifications = specifications.ToDictionary(pair => pair.Key, pair => pair.Value)
            };
            product.DeliveryMethods.Add(deliveryMethods[0]);
            product.DeliveryMethods.Add(deliveryMethods[1]);
            product.DeliveryMethods.Add(deliveryMethods[2]);
            return product;
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}