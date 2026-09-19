using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.CompanyBranch;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(b => new { b.CompanyId, b.Code }).IsUnique();

        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.ShortName).HasMaxLength(50);
        builder.Property(b => b.Address).HasMaxLength(250);
        builder.Property(b => b.City).HasMaxLength(100);
        builder.Property(b => b.State).HasMaxLength(100);
        builder.Property(b => b.PostalCode).HasMaxLength(20);
        builder.Property(b => b.Country).HasMaxLength(100);
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.Email).HasMaxLength(100);
        builder.Property(b => b.OpeningDate);
        builder.Property(b => b.IsHeadOffice).HasDefaultValue(false);

        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);

        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BranchType)
            .WithMany(bt => bt.Branches)
            .HasForeignKey(b => b.BranchTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Settings)
            .WithOne(s => s.Branch)
            .HasForeignKey<BranchSettings>(s => s.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}