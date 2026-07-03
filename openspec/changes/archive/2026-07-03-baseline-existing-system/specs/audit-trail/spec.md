## ADDED Requirements

### Requirement: Entity changes to auditable entities are captured automatically
`AuditSaveChangesInterceptor` SHALL automatically create an audit log entry for every `Added`, `Modified`, or `Deleted` change to an `IAuditable` entity on `SaveChanges`, without requiring each command handler to remember to log it explicitly.

#### Scenario: Creating a GL entry is audited without explicit logging code
- **WHEN** a new `GLEntry` is added to the DbContext and `SaveChangesAsync` is called
- **THEN** an audit log row is created with `Action = "Create"`, `EntityName = "GLEntry"`, `EntityId` set, `BeforeValuesJson = null`, and `AfterValuesJson` containing the serialized new values

#### Scenario: Deleting an entity captures its prior state, not its new state
- **WHEN** an auditable entity is deleted
- **THEN** the audit entry has `Action = "Delete"`, `BeforeValuesJson` containing the entity's last state, and `AfterValuesJson = null`

### Requirement: Every automatic audit entry records who, from where, and when
Each interceptor-captured audit entry SHALL include `UserId`, `IpAddress`, and `UserAgent` (from `ICurrentUserService`) and `TimestampUtc` (from `IDateTimeProvider`), in addition to the entity change itself.

#### Scenario: An anonymous background process cannot produce an untraceable audit entry
- **WHEN** any auditable change is saved
- **THEN** the resulting audit row always has a non-null `TimestampUtc`, and `UserId`/`IpAddress`/`UserAgent` reflect the acting request's `ICurrentUserService` context

### Requirement: Semantic business actions can be explicitly audited beyond raw CRUD
`IAuditLogger.LogAsync` SHALL allow command handlers to record domain-specific actions (e.g. "Approve" when a GL entry is posted, "Void" with a reason) that aren't simply Create/Update/Delete, including an optional `reason`, added to the same unit of work as the entity change so both persist atomically together.

#### Scenario: Posting a GL entry is audited as "Approve", not generically as "Update"
- **WHEN** a Draft GL entry is posted
- **THEN** the audit log records `Action = "Approve"` (via explicit `IAuditLogger.LogAsync` call in the posting handler) rather than the generic `"Update"` the interceptor alone would have produced

### Requirement: Audit log listing supports filtering and is capped at 200 results
`GET` audit logs (`AdminAuditLogsController`, requiring `Admin.AuditLogs.Read`) SHALL support filtering by `entityName`, `entityId`, `userId`, `action`, and a `dateFrom`/`dateTo` range, ordered descending by `TimestampUtc`, and SHALL cap results at the 200 most recent matches.

#### Scenario: Filtering by entity and date range narrows results
- **WHEN** the audit log list is queried with `entityName=GLEntry&dateFrom=2026-06-01&dateTo=2026-06-30`
- **THEN** only GLEntry-related audit rows with `TimestampUtc` in that range are returned, newest first

#### Scenario: More than 200 matching rows still returns only the 200 most recent
- **WHEN** more than 200 audit rows match the given filters
- **THEN** only the 200 most recent (by `TimestampUtc` descending) are returned — further pagination is deferred to a later phase
