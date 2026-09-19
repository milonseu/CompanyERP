using CompanyERP.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierEntity = CompanyERP.Entities.Supplier.Supplier;

namespace CompanyERP.Data.Configurations.Supplier;

public class SupplierConfiguration : IEntityTypeConfiguration<SupplierEntity>
{
    public void Configure(EntityTypeBuilder<SupplierEntity> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SupplierCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => new { s.CompanyId, s.SupplierCode }).IsUnique();

        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.LegalName).HasMaxLength(200);
        builder.Property(s => s.ContactPerson).HasMaxLength(100);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(100);
        builder.Property(s => s.Website).HasMaxLength(200);
        builder.Property(s => s.Fax).HasMaxLength(30);
        builder.Property(s => s.OpeningPayable).HasPrecision(18, 2);
        builder.Property(s => s.OpeningBalanceDate);
        builder.Property(s => s.Note).HasMaxLength(500);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);

        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Contacts)
            .WithOne(c => c.Supplier)
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Addresses)
            .WithOne(a => a.Supplier)
            .HasForeignKey(a => a.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}