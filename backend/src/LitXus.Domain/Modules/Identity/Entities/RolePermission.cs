namespace LitXus.Domain.Modules.Identity.Entities;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    internal static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission { RoleId = roleId, PermissionId = permissionId };
    }
}
