using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Employee;
using EmployeeEntity = CompanyERP.Entities.Employee.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Employee;

public class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(e => e.EmployeeCode).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Gender).HasMaxLength(20).HasConversion<string>();
        builder.Property(e => e.DateOfBirth);
        builder.Property(e => e.Mobile).HasMaxLength(30);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.NationalId).HasMaxLength(30);
        builder.Property(e => e.FatherName).HasMaxLength(150);
        builder.Property(e => e.MotherName).HasMaxLength(150);
        builder.Property(e => e.PresentAddress).HasMaxLength(250);
        builder.Property(e => e.PermanentAddress).HasMaxLength(250);
        builder.Property(e => e.JoiningDate).IsRequired();

        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Designation)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DefaultBranch)
            .WithMany()
            .HasForeignKey(e => e.DefaultBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SalaryStructure)
            .WithOne(s => s.Employee)
            .HasForeignKey<SalaryStructure>(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.BranchAssignments)
            .WithOne(a => a.Employee)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.SalaryPayments)
            .WithOne(p => p.Employee)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AssetAssignments)
            .WithOne(a => a.Employee)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}