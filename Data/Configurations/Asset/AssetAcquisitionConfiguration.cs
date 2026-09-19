using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetAcquisitionConfiguration : IEntityTypeConfiguration<AssetAcquisition>
{
    public void Configure(EntityTypeBuilder<AssetAcquisition> builder)
    {
        builder.ToTable("AssetAcquisitions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AmountPaid).HasPrecision(18, 2);
        builder.Property(a => a.PaymentReference).HasMaxLength(100);
        builder.Property(a => a.Note).HasMaxLength(500);
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(a => a.AssetRegisterId).IsUnique();
        builder.HasOne(a => a.Supplier).WithMany().HasForeignKey(a => a.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}