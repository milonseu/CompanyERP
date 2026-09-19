using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("AssetTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(300);
        builder.Property(t => t.SalvageValue).HasPrecision(18, 2);
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(t => new { t.CompanyId, t.Code }).IsUnique();
        builder.HasOne(t => t.Company).WithMany().HasForeignKey(t => t.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(t => t.Assets).WithOne(a => a.AssetType).HasForeignKey(a => a.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}