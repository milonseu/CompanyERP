using CompanyERP.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Company;

public class FinancialYearConfiguration : IEntityTypeConfiguration<FinancialYear>
{
    public void Configure(EntityTypeBuilder<FinancialYear> builder)
    {
        builder.ToTable("FinancialYears");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.YearCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(f => f.YearCode).IsUnique();

        builder.Property(f => f.Name).HasMaxLength(50).IsRequired();
        builder.Property(f => f.StartDate).IsRequired();
        builder.Property(f => f.EndDate).IsRequired();
        builder.Property(f => f.IsClosed).HasDefaultValue(false);

        builder.Property(f => f.CreatedBy).HasMaxLength(100);
        builder.Property(f => f.UpdatedBy).HasMaxLength(100);

        builder.HasMany(f => f.AccountingPeriods)
            .WithOne(p => p.FinancialYear)
            .HasForeignKey(p => p.FinancialYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}