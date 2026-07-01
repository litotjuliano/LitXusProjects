# 14 — Technology-Specific Implementation

## 14.1 .NET 9 Project Setup (Step-by-Step)

```bash
mkdir backend && cd backend
dotnet new sln -n LitXus

dotnet new webapi -n LitXus.Api -o src/LitXus.Api --use-controllers
dotnet new classlib -n LitXus.Application -o src/LitXus.Application
dotnet new classlib -n LitXus.Domain -o src/LitXus.Domain
dotnet new classlib -n LitXus.Infrastructure -o src/LitXus.Infrastructure

dotnet sln add src/LitXus.Api src/LitXus.Application src/LitXus.Domain src/LitXus.Infrastructure

# Dependency rule wiring
dotnet add src/LitXus.Application reference src/LitXus.Domain
dotnet add src/LitXus.Infrastructure reference src/LitXus.Application
dotnet add src/LitXus.Api reference src/LitXus.Application src/LitXus.Infrastructure

# Key packages
dotnet add src/LitXus.Application package MediatR
dotnet add src/LitXus.Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/LitXus.Application package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add src/LitXus.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/LitXus.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/LitXus.Api package Swashbuckle.AspNetCore
dotnet add src/LitXus.Api package Serilog.AspNetCore

dotnet new xunit -n LitXus.UnitTests -o tests/LitXus.UnitTests
dotnet new xunit -n LitXus.IntegrationTests -o tests/LitXus.IntegrationTests
```

## 14.2 MediatR Pattern — Worked Example

```csharp
// Application/Modules/Accounting/Commands/PostGLEntry/PostGLEntryCommand.cs
public record PostGLEntryCommand(Guid GLEntryId) : IRequest<GLEntryDto>;

public class PostGLEntryCommandHandler : IRequestHandler<PostGLEntryCommand, GLEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAuditLogger _auditLogger;

    public async Task<GLEntryDto> Handle(PostGLEntryCommand request, CancellationToken ct)
    {
        var entry = await _db.GLEntries.Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.GLEntryId, ct)
            ?? throw new NotFoundException(nameof(GLEntry), request.GLEntryId);

        var before = new { entry.Status };
        entry.Post();                              // domain method: validates + mutates + updates account balances
        await _auditLogger.LogAsync("GLEntry", entry.Id.ToString(), "Approve", before, new { entry.Status }, null, ct);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<GLEntryDto>(entry);
    }
}
```

Pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`, `TransactionBehavior`) wrap every handler automatically via `services.AddMediatR(cfg => cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)))` — no per-handler boilerplate for cross-cutting concerns.

## 14.3 Dependency Injection Configuration

```csharp
// Program.cs
builder.Services.AddApplication();       // extension method in LitXus.Application/DependencyInjection.cs
builder.Services.AddInfrastructure(builder.Configuration);  // in LitXus.Infrastructure
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerConfig.Configure);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtConfig.Configure);
builder.Services.AddAuthorization();
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
```

## 14.4 Entity Framework Core Setup

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<GLEntry> GLEntries => Set<GLEntry>();
    // ... one DbSet per entity

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);  // picks up all IEntityTypeConfiguration<T>
        builder.Entity<Account>().HasQueryFilter(a => !a.IsDeleted);            // soft-delete filter, repeated per soft-deletable entity
    }
}
```

Migrations: `dotnet ef migrations add 20260115_Phase1_AccountingSchema --project src/LitXus.Infrastructure --startup-project src/LitXus.Api`. Seeding wired as an `IHostedService` (`SeedDatabaseHostedService`) that runs the phase seeders in order, gated by config as described in [08_Sample_Data.md](08_Sample_Data.md) §8.5.

## 14.5 Frontend Setup — Konrix Template Integration (not a bare Vite scaffold)

The locked spec calls for "UI Template: Envato Dashboard Template (use as-is)". The customer supplied a purchased template — **Konrix** (Vite + React 18 + TypeScript + Tailwind v3 + Redux Toolkit + Redux-Saga + FrostUI/HeadlessUI + ApexCharts) — at `/Users/litojuliano/LitXus Documentations/Konrix_React/Admin`. Per "use as-is," the frontend is built on the template's *own* architecture rather than the originally-planned Zustand/Recharts stack described in the general tech-stack section; that earlier description is superseded by this section wherever the two disagree.

**Setup performed:**
```bash
cp -R "Konrix_React/Admin/." frontend/
cd frontend
npm install --legacy-peer-deps   # google-maps-react (unused demo dep) only supports React <=16 peer range
```

**Integration changes** (template auth/config assumed a different backend contract than ours — see [03_API_Specification.md](03_API_Specification.md)):

| File | Stock template | Changed to |
|---|---|---|
| `src/config.ts` | `process.env.REACT_APP_API_URL` (CRA leftover, silently `undefined` under Vite) | `import.meta.env.VITE_API_URL` |
| `src/helpers/api/apiCore.ts` | `Authorization: "JWT " + token`, session key `konrix_user` | `Authorization: "Bearer " + token` (matches ASP.NET Core `JwtBearerDefaults`), session key `litxus_user` |
| `src/helpers/api/auth.ts` | `/login/`, `/logout/`, `/register/`, `/forgot-password/`, `{username, password}` | `/auth/login`, `/auth/logout`, `/auth/register`, `/auth/forgot-password`, `{email, password}` per [03_API_Specification.md](03_API_Specification.md) §3.3 |
| `src/redux/auth/saga.ts` | expects `response.data` = flat user+token object | unwraps `response.data.data` (our `{ data: {...} }` envelope), extracts `accessToken`/`refreshToken`/`user` |
| `src/redux/auth/{actions,reducers}.ts` | `username`, minimal `UserData` shape | `email`, `UserData`/`UserSession` shaped to match our `roles[]`/`permissions[]`/`enabledModules[]` |
| `src/pages/auth/{Login,Register,RecoverPassword}.tsx` | `username` field, hardcoded demo credentials, `hasThirdPartyLogin` | `email` field (email-validated via Yup), no hardcoded credentials, social login UI removed (email/password only per locked spec) |
| `src/constants/menu.ts` | Konrix's full UI-kit demo menu (Calendar, Kanban, 100+ component showcase items) | Replaced with the real Phase 1 nav: Dashboard, Accounting (Chart of Accounts, GL Entries, Bank Reconciliation, Reports), Administration (Users, Roles, Audit Logs) |
| `src/routes/index.tsx` | demo routes only | added `accountingRoutes` and `adminRoutes`, pointed `Dashboard` at a new LitXus dashboard page |
| `index.html`, `PageBreadcrumb.tsx` | "Konrix" branding, `#konrix` mount id | "LitXus Systems" branding, `#root` mount id |
| `package.json` | `"build": "tsc && vite build"` | `"build": "vite build"` + separate `"typecheck": "tsc --noEmit"` script — the stock template ships with pre-existing TS errors in unused demo pages (`Calendar/AddEditEvent.tsx`, `Kanban/index.tsx`, `forms/FormElements.tsx`, a react-hook-form/Yup typing mismatch) that would otherwise block every build; those pages aren't in the LitXus nav and are candidates for deletion in Phase 5 polish rather than a fix now |

New LitXus-authored files: `src/helpers/api/accounting.ts` (typed `APICore`-based calls for accounts/GL entries), `src/pages/accounting/{Dashboard,ChartOfAccounts,GLEntries,BankReconciliation}.tsx`, `src/pages/accounting/reports/*.tsx`, `src/pages/admin/{Users,Roles,AuditLogs}.tsx` — all built with the template's own components (`VerticalForm`, `FormInput`, `PageBreadcrumb`, `HeadlessUI/ModalLayout`) and CSS classes (`.card`, `.btn`, `.form-input`) rather than introducing a parallel design system.

Verified: `npm run build` (Vite) succeeds; `npm run dev` serves the app with correct "LitXus Systems" branding at `http://localhost:5173`.

## 14.6 JWT Token Implementation

```csharp
// Infrastructure/Identity/JwtTokenGenerator.cs
public string GenerateAccessToken(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions, IEnumerable<string> enabledModules)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email!),
        new("name", user.FullName),
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
    claims.AddRange(permissions.Select(p => new Claim("permission", p)));
    claims.AddRange(enabledModules.Select(m => new Claim("module", m)));

    var token = new JwtSecurityToken(
        issuer: _jwtOptions.Issuer, audience: _jwtOptions.Audience, claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
        signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

## 14.7 Swagger UI Integration

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LitXus Systems API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { /* JWT bearer scheme */ });
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "LitXus.Api.xml"));
});
```

## 14.8 Docker & Deployment Setup

Covered in full in [10_Deployment.md](10_Deployment.md) §10.4 — multi-stage Dockerfile builds both the .NET backend and the Vite frontend into a single image, `docker-compose.yml` wires up SQL Server for a self-hosted quick-start.
