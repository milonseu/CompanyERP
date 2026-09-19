using CompanyERP.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = CompanyERP.Entities.Customer.Customer;

namespace CompanyERP.Data.Configurations.Customer;

public class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => new { c.CompanyId, c.CustomerCode }).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.LegalName).HasMaxLength(200);
        builder.Property(c => c.ContactPerson).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Website).HasMaxLength(200);
        builder.Property(c => c.OpeningReceivable).HasPrecision(18, 2);
        builder.Property(c => c.OpeningBalanceDate);
        builder.Property(c => c.Note).HasMaxLength(500);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);

        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Contacts)
            .WithOne(ct => ct.Customer)
            .HasForeignKey(ct => ct.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Customer)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}