using CompanyERP.Entities.Purchase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Purchase;

public class PurchaseQuotationConfiguration : IEntityTypeConfiguration<PurchaseQuotation>
{
    public void Configure(EntityTypeBuilder<PurchaseQuotation> builder)
    {
        builder.ToTable("PurchaseQuotations");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuotationNo).HasMaxLength(30).IsRequired();
        builder.HasIndex(q => new { q.CompanyId, q.QuotationNo }).IsUnique();
        builder.Property(q => q.Note).HasMaxLength(500);
        builder.Property(q => q.CreatedBy).HasMaxLength(100);
        builder.Property(q => q.UpdatedBy).HasMaxLength(100);
        builder.HasOne(q => q.Company).WithMany().HasForeignKey(q => q.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.Supplier).WithMany().HasForeignKey(q => q.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.PurchaseRequest).WithMany().HasForeignKey(q => q.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(q => q.Lines).WithOne(l => l.PurchaseQuotation).HasForeignKey(l => l.PurchaseQuotationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseQuotationLineConfiguration : IEntityTypeConfiguration<PurchaseQuotationLine>
{
    public void Configure(EntityTypeBuilder<PurchaseQuotationLine> builder)
    {
        builder.ToTable("PurchaseQuotationLines");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.CreatedBy).HasMaxLength(100);
        builder.Property(l => l.UpdatedBy).HasMaxLength(100);
        builder.HasOne(l => l.Product).WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}