using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetDisposalConfiguration : IEntityTypeConfiguration<AssetDisposal>
{
    public void Configure(EntityTypeBuilder<AssetDisposal> builder)
    {
        builder.ToTable("AssetDisposals");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.SaleValue).HasPrecision(18, 2);
        builder.Property(d => d.BookValueAtDisposal).HasPrecision(18, 2);
        builder.Property(d => d.GainLossAmount).HasPrecision(18, 2);
        builder.Property(d => d.Note).HasMaxLength(500);
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(d => d.AssetRegisterId).IsUnique();
    }
}