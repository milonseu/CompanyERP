using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Inventory;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(w => new { w.CompanyId, w.Code }).IsUnique();

        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.Address).HasMaxLength(250);
        builder.Property(w => w.IsActive).HasDefaultValue(true);

        builder.Property(w => w.CreatedBy).HasMaxLength(100);
        builder.Property(w => w.UpdatedBy).HasMaxLength(100);

        builder.HasOne(w => w.Company)
            .WithMany()
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Branch)
            .WithMany()
            .HasForeignKey(w => w.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}