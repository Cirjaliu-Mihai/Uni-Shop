using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniShop.Api.Domain;

namespace UniShop.Api.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(address => address.Id);
        builder.Property(address => address.UserId).HasMaxLength(450).IsRequired();
        builder.Property(address => address.Label).HasMaxLength(80).IsRequired();
        builder.Property(address => address.FullName).HasMaxLength(200).IsRequired();
        builder.Property(address => address.Phone).HasMaxLength(40).IsRequired();
        builder.Property(address => address.Street).HasMaxLength(300).IsRequired();
        builder.Property(address => address.City).HasMaxLength(120).IsRequired();
        builder.Property(address => address.County).HasMaxLength(120).IsRequired();
        builder.Property(address => address.PostalCode).HasMaxLength(6).IsRequired();
        builder.Property(address => address.Country).HasMaxLength(100).IsRequired();
        builder.HasIndex(address => address.UserId);
        builder.HasOne(address => address.User)
            .WithMany(user => user.Addresses)
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}