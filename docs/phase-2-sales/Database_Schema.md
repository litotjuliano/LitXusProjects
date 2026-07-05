# Phase 2 — Database Schema

Reproduced here scoped to this phase so the doc set is self-contained; full ER detail lives in [02_Database_Schema.md](../02_Database_Schema.md) §2.3.

## Migration: `20260705133018_AddSalesModule`

**New tables:**
- `Customers`
- `Invoices`
- `InvoiceLines`
- `Payments`
- `CreditNotes`
- `SalesSettings`

**New sequences:**
- `InvoiceNumberSeq` → formatted `INV-{year}-{seq:D6}`
- `CreditNoteNumberSeq` → formatted `CN-{year}-{seq:D6}`

## Column Notes

**`Customers`:** `Code` (nvarchar(20), unique, immutable after creation), `CompanyName` (200), `ContactPerson`/`Email` (200, nullable), `Phone` (20, nullable), `Address` (500, nullable), `CreditLimit`/`decimal(18,2)`, `PaymentTermsDays` (int), `IsActive` (bit).

**`Invoices`:** `InvoiceNumber` (nvarchar(30), nullable until Issued — unique index is **filtered** `WHERE InvoiceNumber IS NOT NULL`, same pattern as `GLEntries.EntryNumber`), `CustomerId` (FK → `Customers.Id`, `NO ACTION`), `InvoiceDate`/`DueDate` (date), `Status` (int enum: Draft/Issued/PartiallyPaid/Paid/Void), `SubTotal`/`SSTAmount`/`TotalAmount`/`AmountPaid` (`decimal(18,2)`), `Notes` (1000, nullable), `VoidReason` (500, nullable). Soft-delete filtered (`IsDeleted = 0`), matching `Accounts`/`GLEntries`. Indexes on `CustomerId`, `Status`, `InvoiceDate`.

**`InvoiceLines`:** `InvoiceId` (FK → `Invoices.Id`, cascade delete), `Description` (500, required), `Quantity` (`decimal(18,3)`), `UnitOfMeasure` (nvarchar(20), nullable free text — **not** a Product-driven UOM; Phase 3 Inventory replaces this), `UnitPrice`/`LineTotal` (`decimal(18,2)`), `TaxCodeId` (nullable FK → `TaxCodes.Id`, `NO ACTION`). No `ProductId` column — `Products` doesn't exist until Phase 3.

**`Payments`:** `InvoiceId` (FK → `Invoices.Id`), `PaymentDate` (date), `Amount` (`decimal(18,2)`), `Method` (int enum: BankTransfer/Cash/Cheque/OnlineGateway), `ReferenceNumber` (nullable), `Status` (int enum: Pending/Verified/Rejected), `VerifiedBy` (nullable Guid), `VerifiedAtUtc` (nullable datetime2), `BankAccountId` (nullable FK → Phase 1's `BankAccounts.Id`), `RejectReason` (nullable).

**`CreditNotes`:** `CreditNoteNumber` (nvarchar(30), unique filtered same as `Invoices.InvoiceNumber`), `InvoiceId` (FK → `Invoices.Id`), `Reason` (required), `Amount` (`decimal(18,2)`), `Status` (int enum: Draft/Issued/Applied — Phase 2 always writes `Applied` directly).

**`SalesSettings`:** single row (like `Company`/`Licenses`), `DefaultReceivableAccountId`/`DefaultRevenueAccountId`/`DefaultTaxPayableAccountId`/`DefaultCashAccountId` (all nullable Guid FKs → `Accounts.Id` until an Admin configures them).

## Indexes Added

```sql
CREATE UNIQUE INDEX UX_Customers_Code       ON Customers(Code);
CREATE UNIQUE INDEX UX_Invoices_InvoiceNumber ON Invoices(InvoiceNumber) WHERE InvoiceNumber IS NOT NULL;
CREATE INDEX IX_Invoices_CustomerId         ON Invoices(CustomerId);
CREATE INDEX IX_Invoices_Status             ON Invoices(Status);
CREATE INDEX IX_Invoices_InvoiceDate        ON Invoices(InvoiceDate);
CREATE INDEX IX_InvoiceLines_InvoiceId      ON InvoiceLines(InvoiceId);
CREATE UNIQUE INDEX UX_CreditNotes_CreditNoteNumber ON CreditNotes(CreditNoteNumber) WHERE CreditNoteNumber IS NOT NULL;
```

## EF Core Entity Configuration Notes

- `Invoice.Lines`/`InvoiceLine`: owned-style one-to-many with `UsePropertyAccessMode(PropertyAccessMode.Field)`, matching `GLEntry.Lines`' existing convention exactly.
- `Invoice`/`CreditNote`: soft-delete `HasQueryFilter` added alongside `Account`/`GLEntry`'s existing filters.
- No schema changes to any Phase 1 table — purely additive.
