using LitXus.Domain.Modules.Sales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.RejectReason).HasMaxLength(500);

        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.Status);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
