namespace LitXus.Domain.Modules.Identity.Entities;

/// <summary>
/// Custom join, distinct from ASP.NET Identity's AspNetUserRoles — this is what the
/// permission-checking pipeline actually reads. See docs/06_RBAC_Auth.md §6.1 for why both exist.
/// </summary>
public class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole { UserId = userId, RoleId = roleId };
    }
}
