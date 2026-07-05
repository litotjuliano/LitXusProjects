# Phase 2 — API Specification

Base: `/api/v1/sales`. Envelope, status codes, and Swagger conventions per [03_API_Specification.md](../03_API_Specification.md) §3.1–3.2, §3.11. Every controller is `[RequireModule(Module.Sales)]` in addition to the permissions listed below — a deployment not licensed for Sales gets 403 on all of these regardless of role.

## Customers

```
GET    /sales/customers                 ?includeInactive=                        permission: Sales.Customer.Read
POST   /sales/customers                 { code, companyName, contactPerson?, email?, phone?, address?, creditLimit, paymentTermsDays }   permission: Sales.Customer.Create
PUT    /sales/customers/{id}            { companyName, contactPerson?, email?, phone?, address?, creditLimit, paymentTermsDays }          permission: Sales.Customer.Update
                                         (code is immutable — not accepted on update)
PATCH  /sales/customers/{id}/status     { isActive }                             permission: Sales.Customer.Update
```

## Invoices

```
GET    /sales/invoices                  ?status=&customerId=&dateFrom=&dateTo=   permission: Sales.Invoice.Read
GET    /sales/invoices/{id}                                                       permission: Sales.Invoice.Read
POST   /sales/invoices                  { customerId, invoiceDate, dueDate, notes?, lines: [{description, quantity, unitOfMeasure?, unitPrice, taxCodeId?}] }   permission: Sales.Invoice.Create
                                         response meta: { creditLimitWarning: string | null } — never blocks
                                         creation; non-null only if this invoice would push the customer's
                                         outstanding balance past their CreditLimit (0 = no limit configured)
PUT    /sales/invoices/{id}             (Draft only, same shape as POST minus customerId)   permission: Sales.Invoice.Update
POST   /sales/invoices/{id}/issue                                                 permission: Sales.Invoice.Approve
POST   /sales/invoices/{id}/void        { reason }                               permission: Sales.Invoice.Approve
GET    /sales/invoices/{id}/pdf         downloads a PDF rendering of the invoice   permission: Sales.Invoice.Read
POST   /sales/invoices/{id}/payments    { paymentDate, amount, method, referenceNumber?, bankAccountId? }   permission: Sales.Payment.Create
                                         (nested under invoices per the original spec — creates a Pending Payment)
```

## Payments

```
GET    /sales/payments                  ?status=                                 permission: Sales.Payment.Read
POST   /sales/payments/{id}/verify                                                permission: Sales.Payment.Verify
POST   /sales/payments/{id}/reject      { reason }                                permission: Sales.Payment.Verify
                                         (beyond the original 15-endpoint spec — Payment.Status already
                                         includes Rejected, so something has to be able to set it; gated by
                                         the same Verify permission since both are Admin-only actions)
```

## Credit Notes

```
GET    /sales/credit-notes                                                       permission: Sales.CreditNote.Read
                                         (beyond the original 15-endpoint spec — a list view, mirroring the
                                         precedent set by Bank Reconciliation's unmatched-gl-lines addition)
GET    /sales/credit-notes/{id}                                                   permission: Sales.CreditNote.Read
POST   /sales/credit-notes              { invoiceId, reason, amount }             permission: Sales.CreditNote.Create
```

## Sales Settings

```
GET    /sales/settings                                                            permission: Sales.Settings.Update
PUT    /sales/settings                  { receivableAccountId, revenueAccountId, taxPayableAccountId, cashAccountId }   permission: Sales.Settings.Update
```
Not in the original 15-endpoint spec at all — added since the GL account mapping Sales auto-posting needs has to be viewable/configurable from somewhere. Both actions gated by the same permission since only an Admin should ever touch this.

## Reports

```
GET    /sales/reports/sales-summary     ?from=&to=&groupBy=customer|product|month   permission: Sales.Reports.Read
GET    /sales/reports/aging             ?asOfDate=                                  permission: Sales.Reports.Read
```
