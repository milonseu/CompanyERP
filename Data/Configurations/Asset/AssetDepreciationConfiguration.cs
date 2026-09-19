using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetDepreciationConfiguration : IEntityTypeConfiguration<AssetDepreciation>
{
    public void Configure(EntityTypeBuilder<AssetDepreciation> builder)
    {
        builder.ToTable("AssetDepreciations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.PeriodKey).HasMaxLength(7).IsRequired();
        builder.Property(d => d.Amount).HasPrecision(18, 2);
        builder.Property(d => d.AccumulatedAfter).HasPrecision(18, 2);
        builder.Property(d => d.Note).HasMaxLength(300);
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(d => new { d.AssetRegisterId, d.PeriodKey }).IsUnique();
    }
}