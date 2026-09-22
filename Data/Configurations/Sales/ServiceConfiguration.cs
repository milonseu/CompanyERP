using CompanyERP.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Sales;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => new { s.CompanyId, s.Code }).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.UnitPrice).HasPrecision(18, 2);
        builder.Property(s => s.CostPrice).HasPrecision(18, 2);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
        builder.HasOne(s => s.Company).WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}