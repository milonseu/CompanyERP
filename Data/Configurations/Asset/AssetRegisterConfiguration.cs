using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetRegisterConfiguration : IEntityTypeConfiguration<AssetRegister>
{
    public void Configure(EntityTypeBuilder<AssetRegister> builder)
    {
        builder.ToTable("AssetRegisters");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AssetNo).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
        builder.Property(a => a.SerialNo).HasMaxLength(100);
        builder.Property(a => a.Model).HasMaxLength(100);
        builder.Property(a => a.Cost).HasPrecision(18, 2);
        builder.Property(a => a.SalvageValue).HasPrecision(18, 2);
        builder.Property(a => a.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(a => a.Location).HasMaxLength(300);
        builder.Property(a => a.Note).HasMaxLength(500);
        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(a => new { a.CompanyId, a.AssetNo }).IsUnique();
        builder.HasOne(a => a.Company).WithMany().HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Branch).WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Acquisition).WithOne(ac => ac.AssetRegister).HasForeignKey<AssetAcquisition>(ac => ac.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Disposal).WithOne(d => d.AssetRegister).HasForeignKey<AssetDisposal>(d => d.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Assignments).WithOne(x => x.AssetRegister).HasForeignKey(x => x.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Transfers).WithOne(x => x.AssetRegister).HasForeignKey(x => x.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Maintenances).WithOne(x => x.AssetRegister).HasForeignKey(x => x.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Depreciations).WithOne(x => x.AssetRegister).HasForeignKey(x => x.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Documents).WithOne(x => x.AssetRegister).HasForeignKey(x => x.AssetRegisterId).OnDelete(DeleteBehavior.Cascade);
    }
}