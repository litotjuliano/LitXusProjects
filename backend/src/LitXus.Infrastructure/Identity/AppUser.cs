using Microsoft.AspNetCore.Identity;

namespace LitXus.Infrastructure.Identity;

/// <summary>Extends ASP.NET Identity's IdentityUser with the fields docs/02_Database_Schema.md §2.1 requires.</summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
