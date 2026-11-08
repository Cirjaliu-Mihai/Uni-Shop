using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UniShop.Api.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261108120000_UseRemoteSampleProductPhotos")]
public sealed class UseRemoteSampleProductPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Products"
            SET "ImageUrl" = CASE "Name"
                WHEN 'North 14 Laptop' THEN 'https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=900&q=80'
                WHEN 'North 16 Laptop' THEN 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=900&q=80'
                WHEN 'Compact Desktop' THEN 'https://images.unsplash.com/photo-1593640408182-31c70c8268f5?auto=format&fit=crop&w=900&q=80'
                WHEN 'Everyday Phone' THEN 'https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=900&q=80'
                WHEN 'Pro Phone' THEN 'https://images.unsplash.com/photo-1598327105666-5b89351aff97?auto=format&fit=crop&w=900&q=80'
                WHEN 'Fold Phone' THEN 'https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=900&q=80'
                WHEN 'Studio Headphones' THEN 'https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=900&q=80'
                WHEN 'Wireless Earbuds' THEN 'https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?auto=format&fit=crop&w=900&q=80'
                WHEN 'Desk Speaker' THEN 'https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?auto=format&fit=crop&w=900&q=80'
                WHEN 'USB-C Hub' THEN 'https://images.unsplash.com/photo-1625842268584-8f3296236761?auto=format&fit=crop&w=900&q=80'
                WHEN 'Wireless Mouse' THEN 'https://images.unsplash.com/photo-1527814050087-3793815479db?auto=format&fit=crop&w=900&q=80'
                WHEN 'Mechanical Keyboard' THEN 'https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=900&q=80'
                WHEN 'Smart Bulb Set' THEN 'https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=900&q=80'
                WHEN 'Indoor Camera' THEN 'https://images.unsplash.com/photo-1516035069371-29a1b244cc32?auto=format&fit=crop&w=900&q=80'
                WHEN 'Smart Plug' THEN 'https://images.unsplash.com/photo-1558002038-1055907df827?auto=format&fit=crop&w=900&q=80'
            END
            WHERE ("ImageUrl" IS NULL OR "ImageUrl" LIKE 'http://localhost:5000/product-images/%')
              AND (
                ("Name" = 'North 14 Laptop' AND "Description" = 'A portable laptop for work and study.') OR
                ("Name" = 'North 16 Laptop' AND "Description" = 'A larger laptop with a vivid display.') OR
                ("Name" = 'Compact Desktop' AND "Description" = 'A compact desktop for a clean workspace.') OR
                ("Name" = 'Everyday Phone' AND "Description" = 'A reliable smartphone for everyday use.') OR
                ("Name" = 'Pro Phone' AND "Description" = 'A high-performance smartphone with a premium camera.') OR
                ("Name" = 'Fold Phone' AND "Description" = 'A foldable smartphone with a large inner display.') OR
                ("Name" = 'Studio Headphones' AND "Description" = 'Over-ear headphones for focused listening.') OR
                ("Name" = 'Wireless Earbuds' AND "Description" = 'Compact earbuds with a charging case.') OR
                ("Name" = 'Desk Speaker' AND "Description" = 'A compact speaker for desktop audio.') OR
                ("Name" = 'USB-C Hub' AND "Description" = 'A multiport hub for modern laptops.') OR
                ("Name" = 'Wireless Mouse' AND "Description" = 'An ergonomic wireless mouse.') OR
                ("Name" = 'Mechanical Keyboard' AND "Description" = 'A compact mechanical keyboard.') OR
                ("Name" = 'Smart Bulb Set' AND "Description" = 'A pair of app-controlled smart bulbs.') OR
                ("Name" = 'Indoor Camera' AND "Description" = 'A compact camera for home monitoring.') OR
                ("Name" = 'Smart Plug' AND "Description" = 'A remotely controlled outlet with energy monitoring.')
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}