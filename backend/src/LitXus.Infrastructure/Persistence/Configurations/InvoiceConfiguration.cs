using LitXus.Domain.Modules.Sales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30);
        builder.Property(i => i.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(i => i.SSTAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Notes).HasMaxLength(1000);
        builder.Property(i => i.VoidReason).HasMaxLength(500);

        // InvoiceNumber is null until Issued, so the unique index is filtered — same pattern as
        // GLEntry.EntryNumber.
        builder.HasIndex(i => i.InvoiceNumber).IsUnique().HasFilter("[InvoiceNumber] IS NOT NULL");
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.InvoiceDate);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasColumnType("decimal(18,3)");
        builder.Property(l => l.UnitOfMeasure).HasMaxLength(20);
        builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.TaxCode)
            .WithMany()
            .HasForeignKey(l => l.TaxCodeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(l => l.InvoiceId);
    }
}
