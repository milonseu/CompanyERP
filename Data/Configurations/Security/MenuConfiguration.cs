using CompanyERP.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Security;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(m => m.Code).IsUnique();

        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Icon).HasMaxLength(60);
        builder.Property(m => m.Controller).HasMaxLength(60);
        builder.Property(m => m.Action).HasMaxLength(60);
        builder.Property(m => m.Area).HasMaxLength(60);
        builder.Property(m => m.DisplayOrder);
        builder.Property(m => m.IsSystem).HasDefaultValue(false);

        builder.Property(m => m.CreatedBy).HasMaxLength(100);
        builder.Property(m => m.UpdatedBy).HasMaxLength(100);

        builder.HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}