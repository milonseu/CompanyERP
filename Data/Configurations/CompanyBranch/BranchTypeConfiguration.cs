using CompanyERP.Entities.CompanyBranch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.CompanyBranch;

public class BranchTypeConfiguration : IEntityTypeConfiguration<BranchType>
{
    public void Configure(EntityTypeBuilder<BranchType> builder)
    {
        builder.ToTable("BranchTypes");

        builder.HasKey(bt => bt.Id);

        builder.Property(bt => bt.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(bt => bt.Code).IsUnique();

        builder.Property(bt => bt.Name).HasMaxLength(100).IsRequired();
        builder.Property(bt => bt.Description).HasMaxLength(500);

        builder.Property(bt => bt.CreatedBy).HasMaxLength(100);
        builder.Property(bt => bt.UpdatedBy).HasMaxLength(100);
    }
}