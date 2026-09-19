using CompanyERP.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Inventory;

public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");

        builder.HasKey(sb => sb.Id);

        builder.HasIndex(sb => new { sb.ProductId, sb.WarehouseId }).IsUnique();

        builder.Property(sb => sb.Quantity).HasPrecision(18, 3);
        builder.Property(sb => sb.AverageCost).HasPrecision(18, 2);

        builder.Property(sb => sb.CreatedBy).HasMaxLength(100);
        builder.Property(sb => sb.UpdatedBy).HasMaxLength(100);

        builder.HasOne(sb => sb.Product)
            .WithMany()
            .HasForeignKey(sb => sb.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sb => sb.Warehouse)
            .WithMany()
            .HasForeignKey(sb => sb.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}