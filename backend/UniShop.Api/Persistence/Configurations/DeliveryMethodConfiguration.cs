using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class DeliveryMethodConfiguration : IEntityTypeConfiguration<DeliveryMethod>
{
    public void Configure(EntityTypeBuilder<DeliveryMethod> builder)
    {
        builder.ToTable("DeliveryMethods");
        builder.HasKey(method => method.Id);
        builder.Property(method => method.Name).HasMaxLength(120).IsRequired();
        builder.Property(method => method.Price).HasPrecision(10, 2);
        builder.HasIndex(method => method.Name).IsUnique();
        builder.HasMany(method => method.Products)
            .WithMany(product => product.DeliveryMethods)
            .UsingEntity(join => join.ToTable("ProductDeliveryMethods"));
    }
}