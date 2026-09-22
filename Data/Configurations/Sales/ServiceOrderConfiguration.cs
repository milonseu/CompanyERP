using CompanyERP.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Sales;

public class ServiceOrderConfiguration : IEntityTypeConfiguration<ServiceOrder>
{
    public void Configure(EntityTypeBuilder<ServiceOrder> builder)
    {
        builder.ToTable("ServiceOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNo).HasMaxLength(30).IsRequired();
        builder.HasIndex(o => new { o.CompanyId, o.OrderNo }).IsUnique();
        builder.Property(o => o.Quantity).HasPrecision(18, 3);
        builder.Property(o => o.UnitPrice).HasPrecision(18, 2);
        builder.Property(o => o.Note).HasMaxLength(500);
        builder.Property(o => o.CreatedBy).HasMaxLength(100);
        builder.Property(o => o.UpdatedBy).HasMaxLength(100);
        builder.HasOne(o => o.Company).WithMany().HasForeignKey(o => o.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Branch).WithMany().HasForeignKey(o => o.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Service).WithMany().HasForeignKey(o => o.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Deliveries).WithOne(d => d.ServiceOrder).HasForeignKey(d => d.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ServiceDeliveryConfiguration : IEntityTypeConfiguration<ServiceDelivery>
{
    public void Configure(EntityTypeBuilder<ServiceDelivery> builder)
    {
        builder.ToTable("ServiceDeliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DeliveredBy).HasMaxLength(100);
        builder.Property(d => d.Quantity).HasPrecision(18, 3);
        builder.Property(d => d.Note).HasMaxLength(500);
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
        builder.HasOne(d => d.Company).WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.Branch).WithMany().HasForeignKey(d => d.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.ServiceOrder).WithMany(o => o.Deliveries).HasForeignKey(d => d.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}