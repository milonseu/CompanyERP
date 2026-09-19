using CompanyERP.Entities.Asset;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Asset;

public class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        builder.ToTable("AssetDocuments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentName).HasMaxLength(150).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(300);
        builder.Property(d => d.Note).HasMaxLength(500);
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);
    }
}