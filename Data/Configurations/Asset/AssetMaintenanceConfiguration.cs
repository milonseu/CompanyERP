using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetMaintenanceConfiguration : IEntityTypeConfiguration<AssetMaintenance>
{
    public void Configure(EntityTypeBuilder<AssetMaintenance> builder)
    {
        builder.ToTable("AssetMaintenances");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Cost).HasPrecision(18, 2);
        builder.Property(m => m.Vendor).HasMaxLength(150);
        builder.Property(m => m.Description).HasMaxLength(500).IsRequired();
        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);
    }
}