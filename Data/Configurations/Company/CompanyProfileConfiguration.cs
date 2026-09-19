using CompanyERP.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Company;

public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.LegalName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.RegistrationNo).HasMaxLength(50);
        builder.Property(c => c.TaxId).HasMaxLength(50);
        builder.Property(c => c.VatRegistrationNo).HasMaxLength(50);
        builder.Property(c => c.TradeLicenseNo).HasMaxLength(50);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(100);
        builder.Property(c => c.Website).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(250);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(100);
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.Country).HasMaxLength(100);
        builder.Property(c => c.CurrencyCode).HasMaxLength(10).IsRequired();
        builder.Property(c => c.IncorporationDate);
        builder.Property(c => c.FiscalYearStartMonth);

        builder.Property(c => c.CreatedBy).HasMaxLength(100);
        builder.Property(c => c.UpdatedBy).HasMaxLength(100);
    }
}