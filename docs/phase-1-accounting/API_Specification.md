# Phase 1 — API Specification

Base: `/api/v1`. Envelope, status codes, and Swagger conventions per [03_API_Specification.md](../03_API_Specification.md) §3.1–3.2, §3.11.

## Auth

```
POST   /auth/register        { email, password, fullName } -> 201, user status=Pending
POST   /auth/login           { email, password } -> 200 { accessToken, refreshToken, expiresIn, user }
POST   /auth/refresh         { refreshToken } -> 200 new pair
POST   /auth/logout          -> 204
POST   /auth/forgot-password { email } -> 204 (always, to avoid email enumeration)
POST   /auth/reset-password  { token, newPassword } -> 204
GET    /auth/me              -> 200 { id, fullName, email, roles[], permissions[], enabledModules[] }
```

## Admin — Users, Roles, Audit

```
GET    /admin/users                     ?status=&role=&page=&pageSize=
GET    /admin/users/{id}
PATCH  /admin/users/{id}/status         { isActive: bool }   permission: Admin.Users.Update
POST   /admin/users/{id}/roles          { roleId }           permission: Admin.Users.Update
DELETE /admin/users/{id}/roles/{roleId}                       permission: Admin.Users.Update
GET    /admin/roles                                            permission: Admin.Roles.Read
GET    /admin/permissions                                      permission: Admin.Roles.Read
GET    /admin/audit-logs                ?entityName=&entityId=&userId=&dateFrom=&dateTo=&action=&page=
GET    /admin/audit-logs/{id}
```

## Accounting — Chart of Accounts

```
GET    /accounting/accounts             ?type=&parentId=&includeInactive=       permission: Accounting.Account.Read
GET    /accounting/accounts/{id}
POST   /accounting/accounts             { code, name, type, parentAccountId? }   permission: Accounting.Account.Create
PUT    /accounting/accounts/{id}        { name, parentAccountId? }               permission: Accounting.Account.Update
PATCH  /accounting/accounts/{id}/status { isActive }                             permission: Accounting.Account.Update
```

## Accounting — GL Entries

```
GET    /accounting/gl-entries           ?status=&accountId=&dateFrom=&dateTo=&page=   permission: Accounting.GLEntry.Read
GET    /accounting/gl-entries/{id}
POST   /accounting/gl-entries           { entryDate, description, lines: [{accountId, debitAmount, creditAmount, lineDescription?}] }   permission: Accounting.GLEntry.Create
PUT    /accounting/gl-entries/{id}      (Draft only, same shape as POST)                permission: Accounting.GLEntry.Update
POST   /accounting/gl-entries/{id}/post                                                  permission: Accounting.GLEntry.Approve
POST   /accounting/gl-entries/{id}/void { reason }                                       permission: Accounting.GLEntry.Approve
```

## Accounting — Tax

```
GET    /accounting/tax-codes                                    permission: Accounting.TaxCode.Read
POST   /accounting/tax-codes            { code, name, rate, type }   permission: Accounting.TaxCode.Create
POST   /tax/calculate-sst               { subTotal, taxCodeId } -> { sstAmount, total }   permission: Accounting.TaxCode.Read
```

## Accounting — Bank Reconciliation

```
GET    /accounting/bank-accounts                                permission: Accounting.BankAccount.Read
POST   /accounting/bank-accounts        { accountId, bankName, accountNumber }   permission: Accounting.BankAccount.Create
GET    /accounting/bank-accounts/{id}/statement-lines            permission: Accounting.BankAccount.Read
POST   /accounting/bank-accounts/{id}/statement-lines/import      multipart/form-data CSV   permission: Accounting.BankAccount.Update
POST   /accounting/bank-statement-lines/{id}/match                { glEntryLineId }         permission: Accounting.BankAccount.Update
GET    /accounting/bank-accounts/{id}/reconciliation-status                                  permission: Accounting.BankAccount.Read
```

## Accounting — Reports

```
GET    /accounting/reports/trial-balance      ?asOfDate=          permission: Accounting.Reports.Read
GET    /accounting/reports/income-statement   ?from=&to=          permission: Accounting.Reports.Read
GET    /accounting/reports/balance-sheet      ?asOfDate=          permission: Accounting.Reports.Read
GET    /accounting/reports/general-ledger     ?accountId=&from=&to=   permission: Accounting.Reports.Read
GET    /accounting/reports/export             ?report=&format=pdf|excel|csv&...(same filters)   permission: Accounting.Reports.Export
```

## Sample Request/Response — GL Entry Creation & Posting

```jsonc
// POST /api/v1/accounting/gl-entries
{
  "entryDate": "2026-07-01",
  "description": "July rent accrual",
  "lines": [
    { "accountId": "5100-rent-expense-id", "debitAmount": 3500.00, "creditAmount": 0, "lineDescription": "Rent - Shah Alam office" },
    { "accountId": "2100-accrued-liabilities-id", "debitAmount": 0, "creditAmount": 3500.00 }
  ]
}
// 201
{ "data": { "id": "...", "entryNumber": null, "status": "Draft", "entryDate": "2026-07-01", ... } }

// POST /api/v1/accounting/gl-entries/{id}/post
// 200
{ "data": { "id": "...", "entryNumber": "JE-2026-000045", "status": "Posted", "postedAtUtc": "2026-07-01T09:00:00Z" } }

// Unbalanced entry -> 422
{ "error": { "code": "ENTRY_UNBALANCED", "message": "Entry is unbalanced by RM 75.00 (debit exceeds credit)." } }
```

## Error Codes Introduced in Phase 1

| Code | Status | Meaning |
|---|---|---|
| `ENTRY_NOT_DRAFT` | 422 | Attempted to post/edit a non-Draft GL entry |
| `ENTRY_UNBALANCED` | 422 | Debits ≠ credits on post |
| `ENTRY_TOO_FEW_LINES` | 422 | Fewer than 2 lines on post |
| `ACCOUNT_CODE_DUPLICATE` | 409 | Account code already exists |
| `ACCOUNT_INACTIVE` | 422 | GL line references a deactivated account |
| `VOID_REQUIRES_REASON` | 400 | Void called without a reason |
| `STATEMENT_LINE_ALREADY_MATCHED` | 409 | Bank statement line already reconciled |
| `USER_NOT_ACTIVE` | 403 | Login attempt by Pending/deactivated user |
