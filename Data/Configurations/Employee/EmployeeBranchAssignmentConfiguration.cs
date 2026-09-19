using CompanyERP.Entities.CompanyBranch;
using CompanyERP.Entities.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Employee;

public class EmployeeBranchAssignmentConfiguration : IEntityTypeConfiguration<EmployeeBranchAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeBranchAssignment> builder)
    {
        builder.ToTable("EmployeeBranchAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignedDate);
        builder.Property(a => a.IsDefault).HasDefaultValue(false);

        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(a => new { a.EmployeeId, a.BranchId }).IsUnique();

        builder.HasOne(a => a.Branch)
            .WithMany()
            .HasForeignKey(a => a.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}