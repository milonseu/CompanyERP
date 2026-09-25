using CompanyERP.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Payments;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BankName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AccountName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.AccountNo).HasMaxLength(30).IsRequired();
        builder.Property(e => e.OpeningBalance).HasPrecision(18, 2);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(e => new { e.CompanyId, e.AccountNo }).IsUnique();
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}