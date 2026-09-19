using CompanyERP.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Company;

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("AccountingPeriods");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PeriodCode).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.EndDate).IsRequired();
        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(p => new { p.FinancialYearId, p.PeriodCode }).IsUnique();
    }
}