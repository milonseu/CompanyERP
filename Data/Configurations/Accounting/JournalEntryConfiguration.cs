using CompanyERP.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyERP.Data.Configurations.Accounting;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntryNo).HasMaxLength(30).IsRequired();
        builder.Property(e => e.PeriodKey).HasMaxLength(20);
        builder.Property(e => e.SourceModule).HasMaxLength(50);
        builder.Property(e => e.SourceReference).HasMaxLength(50);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.TotalDebit).HasPrecision(18, 2);
        builder.Property(e => e.TotalCredit).HasPrecision(18, 2);
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.HasIndex(e => new { e.CompanyId, e.EntryNo }).IsUnique();
        builder.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Branch).WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.CompanyId, e.BranchId });
        builder.HasOne(e => e.AccountingPeriod).WithMany().HasForeignKey(e => e.AccountingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.Details).WithOne(d => d.JournalEntry).HasForeignKey(d => d.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}