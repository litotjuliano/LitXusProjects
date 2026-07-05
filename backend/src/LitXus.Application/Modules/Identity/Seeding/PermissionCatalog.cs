namespace LitXus.Application.Modules.Identity.Seeding;

public record PermissionDefinition(string Module, string Entity, string Operation)
{
    public string Code => $"{Module}.{Entity}.{Operation}";
}

/// <summary>
/// Generated in code (not hand-typed rows) so the seeded Permissions table can never drift from
/// what [RequirePermission] attributes actually check — see docs/06_RBAC_Auth.md §6.6.
/// Covers Phase 1 (Accounting Pro) and Phase 2 (Sales); Inventory permissions are added in
/// Phase 3 when those controllers exist.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new("Accounting", "Account", "Create"),
        new("Accounting", "Account", "Read"),
        new("Accounting", "Account", "Update"),

        new("Accounting", "GLEntry", "Create"),
        new("Accounting", "GLEntry", "Read"),
        new("Accounting", "GLEntry", "Update"),
        new("Accounting", "GLEntry", "Approve"),

        new("Accounting", "TaxCode", "Read"),
        new("Accounting", "TaxCode", "Create"),

        new("Accounting", "BankAccount", "Read"),
        new("Accounting", "BankAccount", "Create"),
        new("Accounting", "BankAccount", "Update"),

        new("Accounting", "Reports", "Read"),
        new("Accounting", "Reports", "Export"),

        new("Sales", "Customer", "Create"),
        new("Sales", "Customer", "Read"),
        new("Sales", "Customer", "Update"),

        new("Sales", "Invoice", "Create"),
        new("Sales", "Invoice", "Read"),
        new("Sales", "Invoice", "Update"),
        new("Sales", "Invoice", "Approve"),

        new("Sales", "Payment", "Create"),
        new("Sales", "Payment", "Read"),
        new("Sales", "Payment", "Verify"),

        new("Sales", "CreditNote", "Create"),
        new("Sales", "CreditNote", "Read"),

        new("Sales", "Settings", "Update"),

        new("Sales", "Reports", "Read"),

        new("Company", "Profile", "Read"),
        new("Company", "Profile", "Update"),

        new("Admin", "Users", "Read"),
        new("Admin", "Users", "Update"),
        new("Admin", "Roles", "Read"),
        new("Admin", "Roles", "Update"),
        new("Admin", "AuditLogs", "Read"),
        new("Admin", "License", "Read"),
        new("Admin", "License", "Update"),
    ];
}
