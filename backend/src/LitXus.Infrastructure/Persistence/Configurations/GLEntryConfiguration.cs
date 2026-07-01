using LitXus.Domain.Modules.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class GLEntryConfiguration : IEntityTypeConfiguration<GLEntry>
{
    public void Configure(EntityTypeBuilder<GLEntry> builder)
    {
        builder.Property(e => e.EntryNumber).HasMaxLength(30);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.VoidReason).HasMaxLength(500);

        // EntryNumber is null until posted, so the unique index is filtered —
        // docs/phase-1-accounting/Database_Schema.md "EF Core Entity Configuration Notes".
        builder.HasIndex(e => e.EntryNumber).IsUnique().HasFilter("[EntryNumber] IS NOT NULL");
        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Lines)
            .WithOne()
            .HasForeignKey(l => l.GLEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Lines).UsePropertyAccessMode(Microsoft.EntityFrameworkCore.PropertyAccessMode.Field);
    }
}

public class GLEntryLineConfiguration : IEntityTypeConfiguration<GLEntryLine>
{
    public void Configure(EntityTypeBuilder<GLEntryLine> builder)
    {
        builder.Property(l => l.DebitAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.CreditAmount).HasColumnType("decimal(18,2)");
        builder.Property(l => l.LineDescription).HasMaxLength(500);

        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(l => l.AccountId);

        // A line is either a debit or a credit, never both — docs/02_Database_Schema.md §2.2.
        builder.ToTable(t => t.HasCheckConstraint("CK_GLEntryLines_DebitXorCredit", "[DebitAmount] = 0 OR [CreditAmount] = 0"));
    }
}
