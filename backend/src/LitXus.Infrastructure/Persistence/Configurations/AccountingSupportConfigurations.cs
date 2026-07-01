using LitXus.Domain.Modules.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> builder)
    {
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Rate).HasColumnType("decimal(5,2)");
        builder.HasIndex(t => t.Code).IsUnique();
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.Property(b => b.BankName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
    }
}

public class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Amount).HasColumnType("decimal(18,2)");
        builder.HasIndex(l => l.BankAccountId);
    }
}
