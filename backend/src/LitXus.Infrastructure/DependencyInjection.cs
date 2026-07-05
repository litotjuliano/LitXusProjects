using LitXus.Application.Common.Interfaces;
using LitXus.Infrastructure.Identity;
using LitXus.Infrastructure.Persistence;
using LitXus.Infrastructure.Persistence.Interceptors;
using LitXus.Infrastructure.Seeding;
using LitXus.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LitXus.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(configuration.GetConnectionString("Default"));
            options.AddInterceptors(
                sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                sp.GetRequiredService<DomainEventDispatchInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 10;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<INumberSequenceGenerator, NumberSequenceGenerator>();
        services.AddScoped<IIdentityUserService, IdentityUserService>();

        services.Configure<LicensingOptions>(configuration.GetSection(LicensingOptions.SectionName));
        services.AddSingleton<ILicenseKeyVerifier, LicenseKeyVerifier>();

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddSingleton<IReportPdfExporter, QuestPdfReportExporter>();
        services.AddSingleton<IReportExcelExporter, ClosedXmlReportExporter>();
        services.AddSingleton<IInvoicePdfExporter, QuestPdfInvoiceExporter>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IdentityService>();

        // RbacSeeder is also registered as its concrete type so SeedDatabaseHostedService can
        // resolve it alone (AlwaysRun path) without triggering IEnumerable<ISeeder> resolution,
        // which — unlike lazy access — constructs every registered ISeeder implementation up
        // front, including ones with fragile constructor dependencies (e.g. LicenseSeeder needs
        // ILicenseKeyVerifier, which throws if Licensing:PublicKeyPem isn't configured yet).
        services.AddScoped<RbacSeeder>();
        services.AddScoped<ISeeder>(sp => sp.GetRequiredService<RbacSeeder>());
        services.AddScoped<ISeeder, CompanySeeder>();
        services.AddScoped<ISeeder, LicenseSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, AccountingDemoDataSeeder>();
        services.AddScoped<ISeeder, SalesDemoDataSeeder>();
        services.AddHostedService<SeedDatabaseHostedService>();

        return services;
    }
}
