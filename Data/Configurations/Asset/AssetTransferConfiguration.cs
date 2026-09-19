using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetTransferConfiguration : IEntityTypeConfiguration<AssetTransfer>
{
    public void Configure(EntityTypeBuilder<AssetTransfer> builder)
    {
        builder.ToTable("AssetTransfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.FromLocation).HasMaxLength(300);
        builder.Property(t => t.ToLocation).HasMaxLength(300);
        builder.Property(t => t.Note).HasMaxLength(500);
        builder.Property(t => t.CreatedBy).HasMaxLength(100);
        builder.Property(t => t.UpdatedBy).HasMaxLength(100);
        builder.HasOne(t => t.FromBranch).WithMany().HasForeignKey(t => t.FromBranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToBranch).WithMany().HasForeignKey(t => t.ToBranchId).OnDelete(DeleteBehavior.Restrict);
    }
}