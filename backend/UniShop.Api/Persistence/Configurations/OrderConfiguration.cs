using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.UserId).HasMaxLength(450).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(order => order.ContactName).HasMaxLength(200).IsRequired();
        builder.Property(order => order.ContactEmail).HasMaxLength(254).IsRequired();
        builder.Property(order => order.ContactPhone).HasMaxLength(40).IsRequired();
        builder.Property(order => order.ShippingAddressLabel).HasMaxLength(80).IsRequired();
        builder.Property(order => order.ShippingFullName).HasMaxLength(200).IsRequired();
        builder.Property(order => order.ShippingPhone).HasMaxLength(40).IsRequired();
        builder.Property(order => order.ShippingStreet).HasMaxLength(300).IsRequired();
        builder.Property(order => order.ShippingCity).HasMaxLength(120).IsRequired();
        builder.Property(order => order.ShippingCounty).HasMaxLength(120).IsRequired();
        builder.Property(order => order.ShippingPostalCode).HasMaxLength(6).IsRequired();
        builder.Property(order => order.ShippingCountry).HasMaxLength(100).IsRequired();
        builder.Property(order => order.DeliveryMethodName).HasMaxLength(120).IsRequired();
        builder.Property(order => order.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        builder.Property(order => order.DeliveryMethodPrice).HasPrecision(10, 2);
        builder.Property(order => order.Subtotal).HasPrecision(12, 2);
        builder.Property(order => order.DeliveryCost).HasPrecision(10, 2);
        builder.Property(order => order.Total).HasPrecision(12, 2);
        builder.Property(order => order.Notes).HasMaxLength(1000);
        builder.HasIndex(order => new { order.UserId, order.CreatedAt });
        builder.HasOne(order => order.User)
            .WithMany(user => user.Orders)
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(order => order.DeliveryMethod)
            .WithMany()
            .HasForeignKey(order => order.DeliveryMethodId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(order => order.Payment)
            .WithOne(payment => payment.Order)
            .HasForeignKey<Payment>(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}