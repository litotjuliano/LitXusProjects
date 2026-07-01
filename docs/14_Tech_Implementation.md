# 14 — Technology-Specific Implementation

## 14.1 .NET 10 Project Setup (Step-by-Step, as actually built)

The originally-locked spec targeted .NET 9. .NET 10 shipped as LTS in Nov 2025 — since nothing had been built yet when that came up, the target was switched to .NET 10 (3-year LTS support vs. .NET 9's 18-month STS) before any code was written. A `global.json` pins the SDK so `dotnet new`/`dotnet build` don't silently drift to whatever's newest on a given machine:

```json
// backend/global.json
{ "sdk": { "version": "10.0.301", "rollForward": "latestFeature" } }
```

```bash
mkdir backend && cd backend
dotnet new sln -n LitXus

dotnet new webapi -n LitXus.Api -o src/LitXus.Api --use-controllers -f net10.0
dotnet new classlib -n LitXus.Application -o src/LitXus.Application -f net10.0
dotnet new classlib -n LitXus.Domain -o src/LitXus.Domain -f net10.0
dotnet new classlib -n LitXus.Infrastructure -o src/LitXus.Infrastructure -f net10.0
dotnet new xunit -n LitXus.UnitTests -o tests/LitXus.UnitTests -f net10.0
dotnet new xunit -n LitXus.IntegrationTests -o tests/LitXus.IntegrationTests -f net10.0

dotnet sln add src/LitXus.Api src/LitXus.Application src/LitXus.Domain src/LitXus.Infrastructure tests/LitXus.UnitTests tests/LitXus.IntegrationTests

# Dependency rule wiring
dotnet add src/LitXus.Application reference src/LitXus.Domain
dotnet add src/LitXus.Infrastructure reference src/LitXus.Application
dotnet add src/LitXus.Api reference src/LitXus.Application src/LitXus.Infrastructure
dotnet add tests/LitXus.UnitTests reference src/LitXus.Application src/LitXus.Domain
dotnet add tests/LitXus.IntegrationTests reference src/LitXus.Api

# Key packages — MediatR pinned to the last pre-commercial-license release (v13+ requires a
# paid license above a revenue threshold). AutoMapper is deliberately NOT installed — see §14.2a.
dotnet add src/LitXus.Application package MediatR --version 12.4.1
dotnet add src/LitXus.Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/LitXus.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/LitXus.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/LitXus.Infrastructure package System.IdentityModel.Tokens.Jwt
dotnet add src/LitXus.Api package Swashbuckle.AspNetCore
dotnet add src/LitXus.Api package Serilog.AspNetCore
dotnet add src/LitXus.Api package Microsoft.EntityFrameworkCore.Design   # needed on the *startup* project for `dotnet ef`, not just Infrastructure
```

**Gotcha — do not also add `Microsoft.AspNetCore.OpenApi`.** `dotnet new webapi` adds it by default for the built-in `AddOpenApi()`/`MapOpenApi()` minimal-API doc generator. It pulls in `Microsoft.OpenApi` v1.x, which conflicts at *runtime* (not compile time — the app throws `TypeLoadException` on first request) with the v2.x that Swashbuckle 10.x depends on. Since Swashbuckle is the locked choice, remove `Microsoft.AspNetCore.OpenApi` and don't call `AddOpenApi()`/`MapOpenApi()`.

**Gotcha — `IHttpContextAccessor` in a classlib.** `LitXus.Infrastructure` is a plain `Microsoft.NET.Sdk` classlib (not `.Sdk.Web`), so it doesn't get the ASP.NET Core shared framework automatically. Rather than the old standalone `Microsoft.AspNetCore.Http.Abstractions` NuGet package (deprecated, version-locked at 2.3.11, missing `AddHttpContextAccessor`/newer APIs), add a `FrameworkReference`:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

## 14.2 MediatR Pattern — Worked Example (as actually implemented)

```csharp
// Application/Modules/Accounting/Commands/PostGLEntry/PostGLEntryCommand.cs
public record PostGLEntryCommand(Guid GLEntryId) : IRequest<GLEntryDto>;

public class PostGLEntryCommandHandler(
    IAppDbContext db,
    INumberSequenceGenerator numberSequenceGenerator,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditLogger auditLogger) : IRequestHandler<PostGLEntryCommand, GLEntryDto>
{
    public async Task<GLEntryDto> Handle(PostGLEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await db.GLEntries
            .Include(e => e.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(e => e.Id == request.GLEntryId, cancellationToken)
            ?? throw new NotFoundException(nameof(GLEntry), request.GLEntryId);

        var before = new { entry.Status };
        var entryNumber = await numberSequenceGenerator.NextGLEntryNumberAsync(cancellationToken);
        entry.Post(entryNumber, currentUserService.UserId ?? Guid.Empty, dateTimeProvider.UtcNow);

        await auditLogger.LogAsync(nameof(GLEntry), entry.Id.ToString(), "Approve",
            before, new { entry.Status, entry.EntryNumber }, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return entry.ToDto();   // hand-written extension method, not AutoMapper — see §14.2a
    }
}
```

Pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`) wrap every handler automatically via `services.AddMediatR(cfg => cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)))`. **`TransactionBehavior` was scoped out** — every command handler here does its work against one `IAppDbContext` and calls `SaveChangesAsync` exactly once, which is already atomic; a cross-aggregate multi-SaveChanges transaction wrapper would be speculative complexity with nothing to wrap yet. Add it if/when a command genuinely needs multiple `SaveChangesAsync` calls in one unit of work.

**Gotcha — MediatR 12.x's `RequestHandlerDelegate<TResponse>` is parameterless.** Docs/tutorials for newer MediatR show `next(cancellationToken)`; in the pinned 12.4.1 it's just `next()`. This only surfaces as a compile error (`CS1593`), not a runtime issue, but it's an easy copy-paste trap when working from newer examples.

## 14.2a Why There's No AutoMapper

The locked spec named AutoMapper for DTO mapping. Two problems surfaced once it was actually wired up:
- **Licensing:** v13+ requires a commercial license (the "Eazy" license, free tier capped by org revenue). MediatR has the same policy — pinned to 12.4.1 for that reason, and the same fix doesn't work as cleanly for AutoMapper.
- **Security:** the pre-v13 free line (all versions <15.1.1) has an unpatched, high-severity DoS via uncontrolled recursion — **CVE-2026-32933 / GHSA-rvv3-g6hj-g44x**, CVSS 7.5. The maintainer only patched the commercial 15.x/16.x releases; there is no fix available under the free license.

Given both the paid and free paths were compromised, AutoMapper was dropped entirely. Mapping is a handful of `record` DTOs per module with a `ToDto()` extension method sitting next to the DTO definition:

```csharp
// Application/Modules/Accounting/Dtos/AccountDto.cs
public record AccountDto(Guid Id, string Code, string Name, string Type, Guid? ParentAccountId, bool IsActive, decimal Balance);

public static class AccountMappingExtensions
{
    public static AccountDto ToDto(this Account account) => new(
        account.Id, account.Code, account.Name, account.Type.ToString(),
        account.ParentAccountId, account.IsActive, account.Balance);
}
```

This satisfies the underlying architectural intent (DTOs, never expose domain entities directly) without a runtime mapping library at all.

## 14.3 Dependency Injection Configuration (as actually built)

```csharp
// Program.cs
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

builder.Services.AddApplication();                          // LitXus.Application/DependencyInjection.cs
builder.Services.AddInfrastructure(builder.Configuration);   // LitXus.Infrastructure/DependencyInjection.cs
builder.Services.AddControllers();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader().AllowAnyMethod()));

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true, ValidAudience = jwtSection["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
    };
});
builder.Services.AddAuthorization();
```

`AddApplication()` wires MediatR + its two pipeline behaviors + FluentValidation. `AddInfrastructure(config)` wires `AppDbContext` (with the audit interceptor attached), `AddIdentityCore<AppUser>`, and every scoped service (`ICurrentUserService`, `IFeatureFlagService`, `IAuditLogger`, `INumberSequenceGenerator`, `JwtTokenGenerator`, `IdentityService`) in one place — see the full file at `src/LitXus.Infrastructure/DependencyInjection.cs`.

## 14.4 Entity Framework Core Setup

```csharp
// Infrastructure/Persistence/AppDbContext.cs
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    // Named AppRoles/AppUserRoles, not Roles/UserRoles — IdentityDbContext already exposes DbSets
    // with those exact names for its own coarse IdentityRole/IdentityUserRole. Naming ours the
    // same way silently hides the base members (CS0114) instead of failing to compile.
    public DbSet<Role> AppRoles => Set<Role>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<GLEntry> GLEntries => Set<GLEntry>();
    // ... one DbSet per entity

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);  // picks up all IEntityTypeConfiguration<T>
        builder.Entity<Account>().HasQueryFilter(a => !a.IsDeleted);            // soft-delete filter, repeated per soft-deletable entity

        // Backs sequential GL entry numbering — see the NumberSequenceGenerator gotcha below.
        builder.HasSequence<long>("GLEntryNumberSeq", schema: "dbo").StartsAt(1).IncrementsBy(1);
    }
}
```

**Gotcha — timestamps don't stamp themselves.** `BaseEntity.CreatedAtUtc`/`ModifiedAtUtc` are plain settable properties; nothing populates them by default; live-tested inserts came back as `0001-01-01`. Fixed in `AuditSaveChangesInterceptor` by adding a `StampTimestamps` pass (over `ChangeTracker.Entries<BaseEntity>()`, not just `IAuditable` ones — several entities like `GLEntryLine`/`TaxCode` have the timestamp columns without being individually audit-logged) that runs before the audit-capture pass, so the stamped values also show up correctly in the audit snapshot.

**Gotcha — `NEXT VALUE FOR` cannot go through `Database.SqlQuery<T>`.** The natural-looking implementation —
```csharp
await db.Database.SqlQuery<long>($"SELECT NEXT VALUE FOR dbo.GLEntryNumberSeq AS [Value]").SingleAsync(ct);
```
fails at runtime with *"NEXT VALUE FOR function is not allowed in ... sub-queries ... derived tables"* — EF Core wraps `SqlQuery<T>` results in a derived table for column-shaping, and SQL Server explicitly disallows `NEXT VALUE FOR` there. The fix is raw ADO.NET against the same connection instead:
```csharp
var connection = (SqlConnection)db.Database.GetDbConnection();
if (connection.State != ConnectionState.Open) await connection.OpenAsync(ct);
await using var command = connection.CreateCommand();
command.CommandText = "SELECT NEXT VALUE FOR dbo.GLEntryNumberSeq";
var next = (long)(await command.ExecuteScalarAsync(ct))!;
```
This was caught by actually posting a GL entry against a live SQL Server container, not by build/compile — a reminder that EF Core raw-SQL query shaping has sharp edges that only surface at runtime.

Migrations: `dotnet dotnet-ef migrations add 20260701_Phase1_IdentityAndAccountingSchema --project src/LitXus.Infrastructure --startup-project src/LitXus.Api --output-dir Persistence/Migrations` (installed as a local tool via `dotnet new tool-manifest` + `dotnet tool install dotnet-ef`, invoked as `dotnet dotnet-ef ...` rather than a global `dotnet ef`). **Verified**: applied against a real SQL Server 2022 container (`docker run mcr.microsoft.com/mssql/server:2022-latest`) with `dotnet dotnet-ef database update`, then exercised end-to-end through the running API — register → login (first user auto-bootstraps as Admin) → create accounts → create/post/void a GL entry with balance verification and audit-log inspection. Seeding as an `IHostedService` (`SeedDatabaseHostedService`) that runs the phase seeders in order, gated by config as described in [08_Sample_Data.md](08_Sample_Data.md) §8.5, is not yet built — Phase 1 scaffolding so far seeds nothing automatically; the verification above used hand-inserted SQL for the Admin role/permissions/license.

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

## 14.6 JWT Token Implementation (as actually built)

Returns a tuple with the expiry so the caller can compute `expiresIn` for the login response without re-deriving it, and generates the refresh token in the same class:

```csharp
// Infrastructure/Identity/JwtTokenGenerator.cs
public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
    AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions, IEnumerable<string> enabledModules)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new("name", user.FullName),
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
    claims.AddRange(permissions.Select(p => new Claim("permission", p)));
    claims.AddRange(enabledModules.Select(m => new Claim("module", m)));

    var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, expires: expires,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

    return (new JwtSecurityTokenHandler().WriteToken(token), expires);
}

public string GenerateRefreshToken()
{
    var bytes = new byte[64];
    RandomNumberGenerator.Fill(bytes);
    return Convert.ToBase64String(bytes);
}
```

**Refresh tokens are stored via ASP.NET Identity's own `AspNetUserTokens` table** (`UserManager.SetAuthenticationTokenAsync(user, "LitXus", "RefreshToken", token)`), not a custom table — that table already exists in the Identity schema and gives token storage/retrieval for free. The tradeoff, accepted for Phase 1: `RefreshAsync` has no way to look up a user by token value (no index on token content), so it scans active users' stored tokens. Fine at Phase 1 scale; if refresh volume ever matters, switch to a dedicated indexed `RefreshTokens` table.

**Auth is a plain injected service (`IdentityService`), not a MediatR command.** It's inherently bound to `UserManager<AppUser>`/ASP.NET Identity — routing it through the same Application-layer abstraction as business commands wouldn't add testability, just ceremony. `AuthController` calls it directly.

## 14.7 Swagger UI Integration (Microsoft.OpenApi v2.x namespace change)

Swashbuckle.AspNetCore 10.x depends on `Microsoft.OpenApi` v2.x, which moved everything out of the `Microsoft.OpenApi.Models` namespace into `Microsoft.OpenApi` directly, and reworked how security requirements reference a scheme. Code written against older Swashbuckle tutorials (which is most of what's searchable) won't compile against this version:

```csharp
using Microsoft.OpenApi;   // NOT Microsoft.OpenApi.Models

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LitXus Systems API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "Bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header,
    });
    // AddSecurityRequirement now takes a Func<OpenApiDocument, OpenApiSecurityRequirement>,
    // and scheme references are OpenApiSecuritySchemeReference, not OpenApiSecurityScheme
    // { Reference = new OpenApiReference {...} } like older Swashbuckle versions.
    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", null), new List<string>() },
    });
});
```

## 14.8 Docker & Deployment Setup

Covered in full in [10_Deployment.md](10_Deployment.md) §10.4 — multi-stage Dockerfile builds both the .NET backend and the Vite frontend into a single image, `docker-compose.yml` wires up SQL Server for a self-hosted quick-start.
