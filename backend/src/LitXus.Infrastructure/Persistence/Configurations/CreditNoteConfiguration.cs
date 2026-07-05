using LitXus.Domain.Modules.Sales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.Property(c => c.CreditNoteNumber).HasMaxLength(30);
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Amount).HasColumnType("decimal(18,2)");

        builder.HasIndex(c => c.CreditNoteNumber).IsUnique().HasFilter("[CreditNoteNumber] IS NOT NULL");
        builder.HasIndex(c => c.InvoiceId);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(c => c.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
