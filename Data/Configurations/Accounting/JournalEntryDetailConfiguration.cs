using CompanyERP.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Accounting;

public class JournalEntryDetailConfiguration : IEntityTypeConfiguration<JournalEntryDetail>
{
    public void Configure(EntityTypeBuilder<JournalEntryDetail> builder)
    {
        builder.ToTable("JournalEntryDetails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Debit).HasPrecision(18, 2);
        builder.Property(e => e.Credit).HasPrecision(18, 2);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(e => new { e.JournalEntryId, e.AccountId });
        builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}