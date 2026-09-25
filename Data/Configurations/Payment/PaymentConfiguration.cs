using CompanyERP.Entities.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Payments;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PaymentNo).HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceModule).HasMaxLength(50);
        builder.Property(e => e.SourceReferenceNo).HasMaxLength(50);
        builder.Property(e => e.ReferenceNo).HasMaxLength(50);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(e => new { e.CompanyId, e.PaymentNo }).IsUnique();
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ExpenseEntry).WithMany().HasForeignKey(e => e.ExpenseEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.SalaryPayment).WithMany().HasForeignKey(e => e.SalaryPaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.AssetRegister).WithMany().HasForeignKey(e => e.AssetRegisterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PaymentMethod).WithMany().HasForeignKey(e => e.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CashAccount).WithMany().HasForeignKey(e => e.CashAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.BankAccount).WithMany().HasForeignKey(e => e.BankAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}