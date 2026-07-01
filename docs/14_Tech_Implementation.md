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

## 14.5 React + Vite Setup (Step-by-Step)

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install zustand react-router-dom react-hook-form axios dayjs recharts @radix-ui/react-dialog
npm install -D tailwindcss postcss autoprefixer vitest @testing-library/react eslint prettier
npx tailwindcss init -p
```

```ts
// src/shared/api/apiClient.ts
const apiClient = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL });
apiClient.interceptors.request.use(config => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
apiClient.interceptors.response.use(res => res, async error => {
  if (error.response?.status === 401 && !error.config._retry) {
    error.config._retry = true;
    await useAuthStore.getState().refreshTokens();
    return apiClient(error.config);
  }
  return Promise.reject(error);
});
```

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
