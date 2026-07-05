# 03 — API Specification (OpenAPI 3.0)

Base URL: `/api/v1`. All endpoints (except `/auth/*`) require `Authorization: Bearer <jwt>`. All list endpoints support `?page=1&pageSize=25&sort=-createdAt&search=...`. All mutating endpoints are behind `[RequireModule]` + `[RequirePermission]` filters.

## 3.1 Standard Response Envelope

```jsonc
// Success (single)
{ "data": { ...resource }, "meta": null }

// Success (list)
{ "data": [ ...resources ], "meta": { "page": 1, "pageSize": 25, "totalCount": 137, "totalPages": 6 } }

// Error
{ "error": { "code": "VALIDATION_FAILED", "message": "One or more fields are invalid.",
             "details": [ { "field": "invoiceDate", "message": "Invoice date cannot be in the future." } ] } }
```

## 3.2 Status Code Contract

| Code | Meaning | Example |
|---|---|---|
| 200 | OK | GET, successful PUT/PATCH |
| 201 | Created | POST creating a resource |
| 204 | No Content | DELETE (soft-delete), void actions |
| 400 | Validation failed | FluentValidation errors |
| 401 | Not authenticated | Missing/expired JWT |
| 403 | Not authorized | Missing permission or module not licensed |
| 404 | Not found | Resource id doesn't exist / soft-deleted |
| 409 | Conflict | e.g. duplicate invoice number, concurrency token mismatch |
| 422 | Business rule violation | e.g. "Cannot void a paid invoice" |
| 500 | Server error | Unhandled — logged via Serilog, generic message returned |

## 3.3 Authentication Endpoints

```
POST   /api/v1/auth/register          bootstrap-only: succeeds once, for the very first user on a fresh install (auto-granted Super Admin); rejected for every call after that — see 3.4, user creation is Admin-driven from here on
POST   /api/v1/auth/login             { email, password } -> { accessToken, refreshToken, expiresIn, user }
POST   /api/v1/auth/refresh           { refreshToken } -> new token pair (rotates refresh token)
POST   /api/v1/auth/logout            revokes refresh token
GET    /api/v1/auth/me                current user + roles + permissions + enabled modules
```

No self-service password reset endpoint — no email infrastructure exists to deliver a reset token
safely, and returning one to an anonymous caller would be an account-takeover risk. See
`POST /api/v1/admin/users/{id}/reset-password` in §3.4 instead.

## 3.4 Admin / RBAC Endpoints

```
GET    /api/v1/admin/users                          list, filter by role/status
POST   /api/v1/admin/users                          create a user (email, fullName, password, roleId) — active immediately, no Pending state; rejects roleId="Super Admin"
GET    /api/v1/admin/users/{id}
PUT    /api/v1/admin/users/{id}                      edit profile
PATCH  /api/v1/admin/users/{id}/status               activate/deactivate
POST   /api/v1/admin/users/{id}/roles                assign role
DELETE /api/v1/admin/users/{id}/roles/{roleId}        revoke role
POST   /api/v1/admin/users/{id}/reset-password        { newPassword } — sets it immediately, server-side only; rejects a Super Admin target
GET    /api/v1/admin/roles
POST   /api/v1/admin/roles
PUT    /api/v1/admin/roles/{id}
GET    /api/v1/admin/permissions                     full permission catalog (for role editor UI)
GET    /api/v1/admin/audit-logs                      filter by entity, user, date range, action
GET    /api/v1/admin/audit-logs/{id}
GET    /api/v1/admin/license                         current license + enabled modules + expiry
```

## 3.5 Accounting Module (Phase 1) — 20+ endpoints

```
GET    /api/v1/accounting/accounts                   Chart of Accounts, tree or flat (?includeInactive=true to include deactivated)
POST   /api/v1/accounting/accounts                   optional parentAccountId
PUT    /api/v1/accounting/accounts/{id}              rename + reparent — code and type are immutable; rejects circular parent/child
POST   /api/v1/accounting/accounts/{id}/deactivate
POST   /api/v1/accounting/accounts/{id}/reactivate

GET    /api/v1/accounting/gl-entries                 filter: dateFrom, dateTo, status, accountId
GET    /api/v1/accounting/gl-entries/{id}
POST   /api/v1/accounting/gl-entries                  creates Draft entry
PUT    /api/v1/accounting/gl-entries/{id}             replaces date/description/lines — Draft only
POST   /api/v1/accounting/gl-entries/{id}/post        Draft -> Posted (validates balanced dr=cr)
POST   /api/v1/accounting/gl-entries/{id}/void        Posted -> Voided (reason required)

GET    /api/v1/accounting/tax-codes
POST   /api/v1/accounting/tax-codes
POST   /api/v1/accounting/tax/calculate-sst           { subTotal, taxCodeId } -> { sstAmount, total }

GET    /api/v1/accounting/bank-accounts
POST   /api/v1/accounting/bank-accounts
GET    /api/v1/accounting/bank-accounts/{id}/statement-lines
POST   /api/v1/accounting/bank-accounts/{id}/statement-lines/import   CSV upload (Date,Description,Amount)
POST   /api/v1/accounting/bank-statement-lines/{id}/match             { glEntryLineId }
POST   /api/v1/accounting/bank-statement-lines/{id}/unmatch
GET    /api/v1/accounting/bank-accounts/{id}/unmatched-gl-lines
GET    /api/v1/accounting/bank-accounts/{id}/reconciliation-status

GET    /api/v1/accounting/reports/trial-balance       ?asOfDate=
GET    /api/v1/accounting/reports/income-statement     ?from=&to=
GET    /api/v1/accounting/reports/balance-sheet        ?asOfDate=
GET    /api/v1/accounting/reports/general-ledger       ?accountId=&from=&to=

GET    /api/v1/accounting/reports/{report}/pdf         same query params as the JSON endpoint above
GET    /api/v1/accounting/reports/{report}/excel       same query params as the JSON endpoint above
```

`{report}` is one of `trial-balance`, `balance-sheet`, `income-statement`, `general-ledger`. Each pdf/excel
endpoint re-runs the same query, then renders server-side (QuestPDF / ClosedXML) and streams the file back.

CSV export is client-side (each report page builds the file from the JSON above — no `/export`
endpoint; the API is Bearer-token, not cookie, authenticated so a plain download link can't carry the
auth header, and for CSV the data's already in hand from the page's own JSON fetch anyway).

## 3.6 Sales Module (Phase 2) — implemented

Full detail (request/response shapes, permissions) in [phase-2-sales/API_Specification.md](phase-2-sales/API_Specification.md).

```
GET    /api/v1/sales/customers
POST   /api/v1/sales/customers
PUT    /api/v1/sales/customers/{id}
PATCH  /api/v1/sales/customers/{id}/status

GET    /api/v1/sales/invoices                        filter: status, customerId, dateFrom/To
GET    /api/v1/sales/invoices/{id}
POST   /api/v1/sales/invoices                          Draft
PUT    /api/v1/sales/invoices/{id}                      Draft only
POST   /api/v1/sales/invoices/{id}/issue                Draft -> Issued (assigns sequential number)
POST   /api/v1/sales/invoices/{id}/void                 reason required

POST   /api/v1/sales/invoices/{id}/payments
GET    /api/v1/sales/payments                          filter: status
POST   /api/v1/sales/payments/{id}/verify               admin verifies -> updates invoice status
POST   /api/v1/sales/payments/{id}/reject               reason required (beyond original spec — Payment.Status
                                                          already includes Rejected, so something has to set it)

POST   /api/v1/sales/credit-notes
GET    /api/v1/sales/credit-notes/{id}
GET    /api/v1/sales/credit-notes                       list (beyond original spec)

GET    /api/v1/sales/settings                           beyond original spec — GL account mapping needs
PUT    /api/v1/sales/settings                           somewhere to be viewed/configured

GET    /api/v1/sales/reports/sales-summary             ?from=&to=&groupBy=customer|product|month
GET    /api/v1/sales/reports/aging                     accounts receivable aging buckets
```

Not built from the original list: `GET /sales/customers/{id}` (no single-customer detail view exists —
the list view + edit modal cover current usage) and `GET /sales/invoices/{id}/pdf` (no PDF export for
invoices yet, unlike the 4 Accounting reports which do have PDF/Excel export). Both are real gaps for a
future pass, not silently dropped.

## 3.7 Inventory Module (Phase 3) — 12+ endpoints

```
GET    /api/v1/inventory/products
GET    /api/v1/inventory/products/{id}
POST   /api/v1/inventory/products
PUT    /api/v1/inventory/products/{id}
PATCH  /api/v1/inventory/products/{id}/status

GET    /api/v1/inventory/warehouses
POST   /api/v1/inventory/warehouses

GET    /api/v1/inventory/stock-levels                  filter: warehouseId, belowReorderLevel=true
POST   /api/v1/inventory/stock-movements                manual adjustment in/out
GET    /api/v1/inventory/products/{id}/movements        movement history

GET    /api/v1/inventory/reports/valuation              ?asOfDate=&method=FIFO|LIFO|WeightedAvg
GET    /api/v1/inventory/reports/reorder-alert
GET    /api/v1/inventory/reports/stock-movement-summary ?from=&to=
```

## 3.8 Integration Endpoints (Phase 4, Enterprise Pro)

```
GET    /api/v1/admin/feature-flags                     current module enable/disable state
PUT    /api/v1/admin/feature-flags                      toggle modules (validated against license)
GET    /api/v1/integration/gl-posting-rules             view mapping (e.g. Sales Revenue account, SST Payable account)
PUT    /api/v1/integration/gl-posting-rules
POST   /api/v1/integration/gl-posting-rules/test        dry-run: given a sample invoice, show resulting GL entry
```

## 3.9 Events & Training / CPD-style Endpoints

Not in the original 3-product scope from the locked prompt — **intentionally omitted**. (Note: the earlier PSMPE-portal CLAUDE.md context mentions CPD/Events modules; those belong to a different, unrelated project and are not part of LitXus Systems. Excluded here to avoid scope bleed.)

## 3.10 Sample Request/Response

```jsonc
// POST /api/v1/sales/invoices
// Request
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "invoiceDate": "2026-07-01",
  "dueDate": "2026-07-31",
  "lines": [
    { "productId": "9c1a...", "description": "PVC Pipe 4in", "quantity": 100, "unitPrice": 12.50, "taxCodeId": "sst-6-id" }
  ],
  "notes": "Delivery to Shah Alam warehouse"
}

// 201 Response
{
  "data": {
    "id": "b7e2...",
    "invoiceNumber": null,          // assigned only on /issue
    "status": "Draft",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "subTotal": 1250.00,
    "sstAmount": 75.00,
    "totalAmount": 1325.00,
    "lines": [ { "id": "...", "lineTotal": 1250.00 } ]
  },
  "meta": null
}

// 422 Response (business rule)
{
  "error": {
    "code": "INVOICE_ALREADY_PAID",
    "message": "Cannot void an invoice that has verified payments. Issue a credit note instead."
  }
}
```

## 3.11 Swagger/OpenAPI Integration

- Swashbuckle generates the spec from controller XML doc comments + DTO attributes.
- `SwaggerGen` grouped by module tag (`Accounting`, `Sales`, `Inventory`, `Admin`, `Auth`) for a navigable UI.
- JWT bearer auth wired into Swagger UI's "Authorize" button for interactive testing.
- Spec regenerated automatically on build; committed snapshot in `/docs/openapi/openapi.json` updated at the end of each phase for external reference (e.g. Postman import).
