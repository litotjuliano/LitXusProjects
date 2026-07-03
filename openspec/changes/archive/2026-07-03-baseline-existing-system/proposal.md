## Why

LitXus Systems has shipped five real capabilities (Identity/RBAC, Accounting, Company Profile, Licensing, Audit Trail) with no OpenSpec coverage — AI assistants and contributors currently have to re-derive business rules and module behavior from source on every session. This proposal captures the system's current, real behavior as the initial OpenSpec baseline, so `openspec/specs/` becomes a reliable starting point for future change proposals instead of starting from empty.

## What Changes

- Adds `openspec/specs/` coverage for five capabilities that are already fully implemented in code today.
- Documentation only — does not alter any existing behavior, code, or the existing `docs/00-17` human-readable planning docs (which remain the canonical reference; see `docs/00_Overview.md`).
- Does not touch OpenAPI/Swagger (`Program.cs`, `LitXus.Api.csproj`, `docs/03_API_Specification.md`) or any controller.

## Capabilities

### New Capabilities
- `identity-auth`: RBAC roles/permissions, JWT authentication, user/role administration, Super Admin protections.
- `accounting`: Chart of Accounts, GL Entries, and the four financial reports (Trial Balance, Income Statement, Balance Sheet, General Ledger).
- `company-profile`: Company profile and signatories, consumed by the report letterhead.
- `licensing`: RS256-signed license key issuance/verification, offline `LitXus.LicenseGenerator` tool, module enablement.
- `audit-trail`: Audit log capture and admin viewing.

### Modified Capabilities
None — no existing specs yet (this is the first OpenSpec change in the repo).

## Impact

Affected: `openspec/specs/` only (new files). No changes to backend/frontend source code, the OpenAPI/Swagger contract, or `docs/00-17`.
