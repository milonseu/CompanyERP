using CompanyERP.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Payments;

public class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.ToTable("CashAccounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AccountName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AccountCode).HasMaxLength(20).IsRequired();
        builder.Property(e => e.OpeningBalance).HasPrecision(18, 2);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(e => new { e.CompanyId, e.AccountCode }).IsUnique();
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}