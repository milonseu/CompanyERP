using CompanyERP.Entities.Employee;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Employee;

public class EmployeeAssetAssignmentConfiguration : IEntityTypeConfiguration<EmployeeAssetAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeAssetAssignment> builder)
    {
        builder.ToTable("EmployeeAssetAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EmployeeId).IsRequired();
        builder.Property(a => a.AssetCode).HasMaxLength(50);
        builder.Property(a => a.AssetName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(100);
        builder.Property(a => a.AssignedOn).IsRequired();
        builder.Property(a => a.ReturnedOn);
        builder.Property(a => a.Note).HasMaxLength(500);

        builder.Property(a => a.CreatedBy).HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);
    }
}