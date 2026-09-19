using CompanyERP.Entities.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Employee;

public class SalaryPaymentConfiguration : IEntityTypeConfiguration<SalaryPayment>
{
    public void Configure(EntityTypeBuilder<SalaryPayment> builder)
    {
        builder.ToTable("SalaryPayments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EmployeeId).IsRequired();
        builder.Property(p => p.ForMonth).IsRequired();
        builder.Property(p => p.PaymentDate).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasMaxLength(20).HasConversion<string>();
        builder.Property(p => p.PaymentMode).HasMaxLength(20).HasConversion<string>();
        builder.Property(p => p.ReferenceNo).HasMaxLength(50);
        builder.Property(p => p.Note).HasMaxLength(500);

        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.EmployeeId, p.ForMonth }).IsUnique();
    }
}