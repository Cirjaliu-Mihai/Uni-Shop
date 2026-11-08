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
            await SeedCatalogExpansionAsync(db, logger);
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
        await SeedCatalogExpansionAsync(db, logger);

        Product Product(string name, string description, decimal price, int stock, int categoryIndex, int supplierIndex, params (string Key, string Value)[] specifications)
        {
            var product = new Product
            {
                Name = name,
                Description = description,
                Price = price,
                Stock = stock,
                ImageUrl = GetImageUrl(name),
                Category = categories[categoryIndex],
                Supplier = suppliers[supplierIndex],
                Specifications = specifications.ToDictionary(pair => pair.Key, pair => pair.Value)
            };
            product.DeliveryMethods.Add(deliveryMethods[0]);
            product.DeliveryMethods.Add(deliveryMethods[1]);
            product.DeliveryMethods.Add(deliveryMethods[2]);
            return product;
        }

        string? GetImageUrl(string name) => name switch
        {
            "North 14 Laptop" => "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=900&q=80",
            "North 16 Laptop" => "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80",
            "Compact Desktop" => "https://images.unsplash.com/photo-1593640408182-31c70c8268f5?auto=format&fit=crop&w=900&q=80",
            "Everyday Phone" => "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=900&q=80",
            "Pro Phone" => "https://images.unsplash.com/photo-1598327105666-5b89351aff97?auto=format&fit=crop&w=900&q=80",
            "Fold Phone" => "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=900&q=80",
            "Studio Headphones" => "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=80",
            "Wireless Earbuds" => "https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?auto=format&fit=crop&w=900&q=80",
            "Desk Speaker" => "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=900&q=80",
            "USB-C Hub" => "https://images.unsplash.com/photo-1625842268584-8f3296236761?auto=format&fit=crop&w=900&q=80",
            "Wireless Mouse" => "https://images.unsplash.com/photo-1527814050087-3793815479db?auto=format&fit=crop&w=900&q=80",
            "Mechanical Keyboard" => "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=900&q=80",
            "Smart Bulb Set" => "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=900&q=80",
            "Indoor Camera" => "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=900&q=80",
            "Smart Plug" => "https://images.unsplash.com/photo-1558002038-1055907df827?auto=format&fit=crop&w=900&q=80",
            _ => null
        };
    }

    private static async Task SeedCatalogExpansionAsync(AppDbContext db, ILogger logger)
    {
        var categorySeeds = new[]
        {
            new Category { Name = "Gaming", Description = "Consoles and gear for play" },
            new Category { Name = "Wearables", Description = "Smartwatches and fitness trackers" },
            new Category { Name = "Photography", Description = "Cameras and photography gear" }
        };

        foreach (var category in categorySeeds)
        {
            if (!await db.Categories.AnyAsync(item => item.Name == category.Name))
            {
                db.Categories.Add(category);
            }
        }
        await db.SaveChangesAsync();

        var categories = await db.Categories.ToDictionaryAsync(category => category.Name);
        var suppliers = await db.Suppliers.OrderBy(supplier => supplier.Id).ToListAsync();
        var deliveryMethods = await db.DeliveryMethods.OrderBy(method => method.Id).ToListAsync();
        if (suppliers.Count == 0 || deliveryMethods.Count == 0)
        {
            logger.LogWarning("Additional sample products were not seeded because suppliers or delivery methods are missing.");
            return;
        }

        var productSeeds = new[]
        {
            new Product
            {
                Name = "Orbit Game Console", Description = "A living-room console for fast, detailed games.", Price = 2299m, Stock = 7,
                ImageUrl = "https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Gaming"].Id, SupplierId = suppliers[0].Id,
                Specifications = new Dictionary<string, string> { ["Storage"] = "1 TB", ["Resolution"] = "4K" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Arcade Wireless Controller", Description = "A comfortable wireless controller with responsive buttons.", Price = 299m, Stock = 20,
                ImageUrl = "https://images.unsplash.com/photo-1592840496694-26d035b52b48?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Gaming"].Id, SupplierId = suppliers[1 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["Connection"] = "Wireless", ["Battery"] = "Rechargeable" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Tournament Gaming Headset", Description = "A closed-back headset for clear team chat and focused play.", Price = 549m, Stock = 15,
                ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Gaming"].Id, SupplierId = suppliers[2 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["Connection"] = "Wireless", ["Microphone"] = "Detachable" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Summit Smartwatch", Description = "A daily smartwatch for notifications, activity and sleep tracking.", Price = 899m, Stock = 13,
                ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Wearables"].Id, SupplierId = suppliers[0].Id,
                Specifications = new Dictionary<string, string> { ["Display"] = "AMOLED", ["Water resistance"] = "5 ATM" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Active GPS Watch", Description = "A lightweight GPS watch for runs and outdoor training.", Price = 1199m, Stock = 10,
                ImageUrl = "https://images.unsplash.com/photo-1434493789847-2f02dc6ca35d?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Wearables"].Id, SupplierId = suppliers[1 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["GPS"] = "Multi-band", ["Battery"] = "7 days" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Pulse Fitness Band", Description = "A simple fitness band for step, heart-rate and sleep tracking.", Price = 349m, Stock = 24,
                ImageUrl = "https://images.unsplash.com/photo-1508685096489-7aacd43bd3b1?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Wearables"].Id, SupplierId = suppliers[2 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["Battery"] = "10 days", ["Water resistance"] = "5 ATM" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Vista Mirrorless Camera", Description = "A compact mirrorless camera for travel and everyday photography.", Price = 3499m, Stock = 6,
                ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Photography"].Id, SupplierId = suppliers[0].Id,
                Specifications = new Dictionary<string, string> { ["Resolution"] = "24 MP", ["Video"] = "4K" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "Field DSLR Camera", Description = "A dependable DSLR camera for learning and creative work.", Price = 2799m, Stock = 5,
                ImageUrl = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Photography"].Id, SupplierId = suppliers[1 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["Resolution"] = "24 MP", ["Lens mount"] = "Interchangeable" }, DeliveryMethods = deliveryMethods
            },
            new Product
            {
                Name = "35mm Portrait Lens", Description = "A bright prime lens for portraits and low-light photography.", Price = 1599m, Stock = 8,
                ImageUrl = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?auto=format&fit=crop&w=900&q=80",
                CategoryId = categories["Photography"].Id, SupplierId = suppliers[2 % suppliers.Count].Id,
                Specifications = new Dictionary<string, string> { ["Focal length"] = "35 mm", ["Aperture"] = "f/1.8" }, DeliveryMethods = deliveryMethods
            }
        };

        foreach (var product in productSeeds)
        {
            if (!await db.Products.AnyAsync(item => item.Name == product.Name))
            {
                db.Products.Add(product);
            }
        }

        await db.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}