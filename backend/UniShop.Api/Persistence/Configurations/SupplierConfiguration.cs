using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Name).HasMaxLength(160).IsRequired();
        builder.Property(supplier => supplier.ContactEmail).HasMaxLength(254).IsRequired();
        builder.Property(supplier => supplier.Phone).HasMaxLength(40).IsRequired();
        builder.HasMany(supplier => supplier.Products)
            .WithOne(product => product.Supplier)
            .HasForeignKey(product => product.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}