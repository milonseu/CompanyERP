using CompanyERP.Entities.Purchase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Purchase;

public class PurchaseReceivingConfiguration : IEntityTypeConfiguration<PurchaseReceiving>
{
    public void Configure(EntityTypeBuilder<PurchaseReceiving> builder)
    {
        builder.ToTable("PurchaseReceivings");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ReceivingNo).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => new { r.CompanyId, r.ReceivingNo }).IsUnique();
        builder.Property(r => r.ReferenceNo).HasMaxLength(100);
        builder.Property(r => r.Note).HasMaxLength(500);
        builder.Property(r => r.CreatedBy).HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);
        builder.HasOne(r => r.Company).WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.PurchaseOrder).WithMany().HasForeignKey(r => r.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Supplier).WithMany().HasForeignKey(r => r.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Warehouse).WithMany().HasForeignKey(r => r.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(r => r.Lines).WithOne(l => l.PurchaseReceiving).HasForeignKey(l => l.PurchaseReceivingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseReceivingLineConfiguration : IEntityTypeConfiguration<PurchaseReceivingLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReceivingLine> builder)
    {
        builder.ToTable("PurchaseReceivingLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}