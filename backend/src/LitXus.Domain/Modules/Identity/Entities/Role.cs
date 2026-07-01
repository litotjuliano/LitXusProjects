using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Identity.Entities;

public class Role : BaseEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role() { }

    public static Role Create(string name, string? description = null)
    {
        return new Role { Name = name, Description = description };
    }

    public void GrantPermission(Permission permission)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permission.Id)) return;
        _rolePermissions.Add(RolePermission.Create(Id, permission.Id));
    }
}
