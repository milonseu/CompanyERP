using CompanyERP.Entities.CompanyBranch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.CompanyBranch;

public class BranchSettingsConfiguration : IEntityTypeConfiguration<BranchSettings>
{
    public void Configure(EntityTypeBuilder<BranchSettings> builder)
    {
        builder.ToTable("BranchSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.BranchId).IsRequired();
        builder.HasIndex(s => s.BranchId).IsUnique();

        builder.Property(s => s.TransactionPrefix).HasMaxLength(10);
        builder.Property(s => s.DefaultCurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(s => s.DefaultPaymentDays).HasDefaultValue(0);
        builder.Property(s => s.AllowInventory).HasDefaultValue(true);
        builder.Property(s => s.AllowSales).HasDefaultValue(true);
        builder.Property(s => s.AllowPurchase).HasDefaultValue(true);
        builder.Property(s => s.AllowExpense).HasDefaultValue(true);

        builder.Property(s => s.CreatedBy).HasMaxLength(100);
        builder.Property(s => s.UpdatedBy).HasMaxLength(100);
    }
}