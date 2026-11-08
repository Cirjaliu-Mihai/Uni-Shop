using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => new { item.OrderId, item.ProductId });
        builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(12, 2);
        builder.Property(item => item.LineTotal).HasPrecision(12, 2);
        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}