using CompanyERP.Entities.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Employee;

public class SalaryStructureConfiguration : IEntityTypeConfiguration<SalaryStructure>
{
    public void Configure(EntityTypeBuilder<SalaryStructure> builder)
    {
        builder.ToTable("SalaryStructures");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployeeId).IsRequired();
        builder.HasIndex(s => s.EmployeeId).IsUnique();

        builder.Property(s => s.EffectiveFrom).IsRequired();

        builder.Property(s => s.BasicSalary).HasPrecision(18, 2);
        builder.Property(s => s.HouseRent).HasPrecision(18, 2);
        builder.Property(s => s.MedicalAllowance).HasPrecision(18, 2);
        builder.Property(s => s.ConveyanceAllowance).HasPrecision(18, 2);
        builder.Property(s => s.OtherAllowance).HasPrecision(18, 2);
        builder.Property(s => s.ProvidentFundPercent).HasPrecision(5, 2);

        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
    }
}