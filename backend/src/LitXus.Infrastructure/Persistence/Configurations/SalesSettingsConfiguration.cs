using LitXus.Domain.Modules.Sales.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class SalesSettingsConfiguration : IEntityTypeConfiguration<SalesSettings>
{
    public void Configure(EntityTypeBuilder<SalesSettings> builder)
    {
        // Single row per install — no FK constraints to Accounts here (the account IDs are just
        // configuration pointers resolved at GL-posting time, same reasoning as BankAccount.AccountId).
    }
}
