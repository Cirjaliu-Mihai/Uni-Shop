using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        var specifications = builder.Property(product => product.Specifications);
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "\"Stock\" >= 0");
        });
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).HasMaxLength(200).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(4000).IsRequired();
        specifications
            .HasColumnType("jsonb")
            .HasConversion(
                specifications => JsonSerializer.Serialize(specifications, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        specifications.Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!));
        builder.Property(product => product.Price).HasPrecision(12, 2);
        builder.Property(product => product.ImageUrl).HasMaxLength(2048);
        builder.Property(product => product.CreatedAt).IsRequired();
        builder.Property(product => product.UpdatedAt).IsRequired();
        builder.HasIndex(product => product.Name);
        builder.HasIndex(product => product.CategoryId);
    }
}