using LitXus.Domain.Modules.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LitXus.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.TimestampUtc);
        builder.HasIndex(a => a.UserId);
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.Property(l => l.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(l => l.EnabledModules).HasMaxLength(200).IsRequired();
        builder.Property(l => l.IssuedToCompany).HasMaxLength(200).IsRequired();
        // RS256 JWTs run well past 500 chars (header+payload+signature) — a plain-string cap made
        // sense before signed keys existed, but is too small now. 4000 covers a realistically large
        // claim set with headroom.
        builder.Property(l => l.LicenseKey).HasMaxLength(4000).IsRequired();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
    }
}

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.SsmRegistrationNumber).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Tin).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Usid).HasMaxLength(50);
        builder.Property(c => c.BusinessRegistrationNumber).HasMaxLength(50);

        builder.Property(c => c.MsicCode).HasMaxLength(10).IsRequired();
        builder.Property(c => c.PrincipalBusinessActivity).HasMaxLength(500).IsRequired();

        builder.Property(c => c.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(c => c.AddressLine2).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(100).IsRequired();
        builder.Property(c => c.State).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PostalCode).HasMaxLength(10).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(100).IsRequired();

        builder.Property(c => c.Phone).HasMaxLength(30).IsRequired();
        builder.Property(c => c.SecondaryPhone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Website).HasMaxLength(200);

        builder.Property(c => c.PrimaryBankName).HasMaxLength(100);
        builder.Property(c => c.PrimaryBankAccountNumber).HasMaxLength(50);
        builder.Property(c => c.PrimaryBankAccountHolderName).HasMaxLength(200);
        builder.Property(c => c.PrimaryBankSwiftCode).HasMaxLength(20);

        builder.Property(c => c.SstRegistrationNumber).HasMaxLength(30);
        builder.Property(c => c.EisNumber).HasMaxLength(30);
        builder.Property(c => c.EpfNumber).HasMaxLength(30);
        builder.Property(c => c.SocsoNumber).HasMaxLength(30);

        builder.Property(c => c.ExternalAuditorName).HasMaxLength(200);
        builder.Property(c => c.CompanySecretaryName).HasMaxLength(200);
    }
}

public class CompanySignatoryConfiguration : IEntityTypeConfiguration<CompanySignatory>
{
    public void Configure(EntityTypeBuilder<CompanySignatory> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.IcNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Position).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Email).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(30);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
