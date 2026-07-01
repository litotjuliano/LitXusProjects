namespace LitXus.Domain.Modules.Identity.Entities;

/// <summary>
/// Module/Entity/Operation catalog is generated in code from PermissionCatalog (Application layer)
/// so the seeded rows can never drift from what RequirePermissionAttribute actually checks —
/// see docs/06_RBAC_Auth.md §6.6.
/// </summary>
public class Permission
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Module { get; private set; } = string.Empty;
    public string Entity { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    private Permission() { }

    /// <summary>Code format is {Module}.{Entity}.{Operation}, e.g. "Accounting.GLEntry.Create" — docs/06_RBAC_Auth.md §6.2.</summary>
    public static Permission Create(string module, string entity, string operation)
    {
        return new Permission
        {
            Module = module,
            Entity = entity,
            Operation = operation,
            Code = $"{module}.{entity}.{operation}",
        };
    }
}
