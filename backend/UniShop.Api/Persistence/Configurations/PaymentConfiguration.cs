using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.ProviderReference).HasMaxLength(200);
        builder.HasIndex(payment => payment.OrderId).IsUnique();
    }
}