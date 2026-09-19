using CompanyERP.Entities.Supplier;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Supplier;

public class SupplierAddressConfiguration : IEntityTypeConfiguration<SupplierAddress>
{
    public void Configure(EntityTypeBuilder<SupplierAddress> builder)
    {
        builder.ToTable("SupplierAddresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AddressType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.AddressLine).HasMaxLength(250).IsRequired();
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.PostalCode).HasMaxLength(20);
        builder.Property(a => a.Country).HasMaxLength(100);
        builder.Property(a => a.IsPrimary).HasDefaultValue(false);

        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);
    }
}