# 07 — Audit Trail Implementation

## 7.1 What Gets Audited

Everything that changes state or accesses sensitive data:
- All CRUD on Accounting, Sales, Inventory entities (GL entries, invoices, payments, products, stock movements...)
- User logins (success and failure), logouts, password resets
- Permission/role changes (who granted what, to whom)
- Approval actions (GL posting, payment verification, invoice void)
- Configuration changes (feature flag toggles, GL posting rule edits)
- Report exports (who exported what report, when — for financial-data-leaves-the-system traceability)

Explicitly NOT audited at row level: read-only GET list/detail requests (would be excessive volume for little value) — but report *exports* are audited since that's data leaving the system.

## 7.2 Audit Log Table Structure

Already introduced in [02_Database_Schema.md](02_Database_Schema.md) §2.1:

```sql
AuditLogs
  Id                uniqueidentifier PK
  EntityName        nvarchar(100)     -- "GLEntry", "Invoice", "User"
  EntityId          nvarchar(100)     -- string form of the entity's PK
  Action            nvarchar(20)      -- Create | Update | Delete | Approve | Void | Login | LoginFailed | PermissionChange | Export
  BeforeValuesJson  nvarchar(max)     -- null for Create
  AfterValuesJson   nvarchar(max)     -- null for Delete
  Reason            nvarchar(500)     -- required for Delete/Void/PermissionChange, optional otherwise
  UserId            uniqueidentifier  -- null for system/anonymous actions (e.g. failed login before identification)
  IpAddress         nvarchar(45)
  UserAgent         nvarchar(500)
  TimestampUtc      datetime2
```

## 7.3 How It's Captured — EF Core SaveChanges Interceptor

Rather than sprinkling manual audit-log-writing calls through every command handler (error-prone, easy to forget), auditing is centralized in an `IInterceptor`:

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        var context = eventData.Context!;
        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            auditEntries.Add(new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = entry.Property("Id").CurrentValue!.ToString()!,
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Deleted => "Delete",
                    _ => "Update"
                },
                BeforeValuesJson = entry.State != EntityState.Added
                    ? JsonSerializer.Serialize(entry.OriginalValues.ToObject()) : null,
                AfterValuesJson = entry.State != EntityState.Deleted
                    ? JsonSerializer.Serialize(entry.CurrentValues.ToObject()) : null,
                UserId = _currentUserService.UserId,
                IpAddress = _currentUserService.IpAddress,
                UserAgent = _currentUserService.UserAgent,
                TimestampUtc = _dateTimeProvider.UtcNow
            });
        }

        context.Set<AuditLog>().AddRange(auditEntries);
        return base.SavingChanges(eventData, result);
    }
}
```

Domain entities implement `IAuditable` (marker interface) to opt in — this is every financial/business entity, not framework/lookup tables. Login events and permission changes are audited explicitly in their respective command handlers (not row changes, so the interceptor doesn't naturally see them) via a shared `IAuditLogger.LogAsync(...)`.

## 7.4 Immutability Enforcement

- **Application-level:** No controller, command, or handler ever issues an UPDATE or DELETE against `AuditLogs`. There is no `UpdateAuditLogCommand`.
- **Database-level (defense in depth):** The SQL login the application connects with is granted `INSERT`, `SELECT` on `AuditLogs` only — `UPDATE`/`DELETE` are explicitly not granted, so even a bug or a compromised app-layer credential cannot alter history without a separate DBA-level credential.
- **Admin "delete" is logged, not real:** if an admin needs to redact something (e.g. accidental PII), that action itself is audited as a new row, and the mechanism is an admin-only, additionally-logged stored procedure — not exposed via the normal API.

## 7.5 Retention (7-Year Policy)

- No automatic deletion job for the first 7 years — audit logs are simply retained by default (Malaysia compliance minimum).
- After 7 years, a manual, admin-triggered archival process moves rows older than the cutoff to cold storage (exported to encrypted CSV/Parquet in customer-controlled storage) rather than automatic deletion — the customer retains control over their own compliance posture.
- `AuditLogs.TimestampUtc` index (already in §2.6 of [02_Database_Schema.md](02_Database_Schema.md)) supports efficient range queries for both reporting and eventual archival.

## 7.6 Query/Reporting

`GET /api/v1/admin/audit-logs?entityName=Invoice&entityId=...&userId=...&dateFrom=&dateTo=&action=` — paginated, admin-only (`Admin.AuditLogs.Read` permission). UI: filterable table, click a row to see a before/after diff view (JSON pretty-printed, changed fields highlighted).

## 7.7 Malaysia Compliance Considerations

- PDPA: audit logs necessarily contain personal data (who did what) — access to the audit log viewer itself is permission-gated and is itself audited when viewed by non-owner-of-the-data admins, to satisfy accountability principle.
- Companies Act 2016 / tax audit requirements: GL entry audit trail must show the full lifecycle (Draft→Posted→Voided) with who/when/why for each transition — covered by capturing `Action` as a distinct value per transition rather than collapsing everything into generic "Update".

## 7.8 Worked Example — Auditing a GL Entry

```
1. Accountant creates GL entry (Draft) -> AuditLogs row: Action=Create, AfterValuesJson={status: Draft, lines: [...]}
2. Accountant posts it -> command handler explicitly calls IAuditLogger.LogAsync(
     entity: "GLEntry", id, action: "Approve" /* semantic, not generic Update */,
     before: {status: Draft}, after: {status: Posted}, reason: null)
   (interceptor would only say "Update" — the handler adds the more meaningful "Approve"
   action in addition, since posting is a business-significant transition worth its own audit action)
3. Six months later, someone voids it -> Action=Void, Reason="Duplicate entry, see JE-2026-000456",
   before: {status: Posted}, after: {status: Voided}
4. Admin audit log viewer shows all 3 rows chronologically against EntityId = that GL entry's Id
```
