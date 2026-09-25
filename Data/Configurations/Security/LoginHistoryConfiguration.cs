using CompanyERP.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Security;

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        builder.ToTable("LoginHistories");

        builder.HasKey(lh => lh.Id);

        builder.Property(lh => lh.UserName).HasMaxLength(50).IsRequired();
        builder.Property(lh => lh.LoginTime);
        builder.Property(lh => lh.LogoutTime);
        builder.Property(lh => lh.IsSuccess).HasDefaultValue(false);
        builder.Property(lh => lh.IpAddress).HasMaxLength(50);
        builder.Property(lh => lh.FailReason).HasMaxLength(500);

        builder.Property(lh => lh.CreatedBy).HasMaxLength(100);
        builder.Property(lh => lh.UpdatedBy).HasMaxLength(100);

        builder.HasIndex(lh => lh.LoginTime);
    }
}