using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UniShop.Api.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261108110000_AddSampleProductPhotos")]
public sealed class AddSampleProductPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Products"
            SET "ImageUrl" = CASE "Name"
                WHEN 'North 14 Laptop' THEN 'http://localhost:5000/product-images/laptop.jpg'
                WHEN 'Everyday Phone' THEN 'http://localhost:5000/product-images/phone.jpg'
                WHEN 'Studio Headphones' THEN 'http://localhost:5000/product-images/headphones.jpg'
            END
            WHERE "ImageUrl" IS NULL AND (
                ("Name" = 'North 14 Laptop' AND "Description" = 'A portable laptop for work and study.') OR
                ("Name" = 'Everyday Phone' AND "Description" = 'A reliable smartphone for everyday use.') OR
                ("Name" = 'Studio Headphones' AND "Description" = 'Over-ear headphones for focused listening.')
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}