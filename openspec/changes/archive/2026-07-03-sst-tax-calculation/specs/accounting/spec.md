## ADDED Requirements

### Requirement: Tax codes can be listed and created via the API
`GET /api/v1/accounting/tax-codes` (permission `Accounting.TaxCode.Read`) SHALL return all tax codes. `POST /api/v1/accounting/tax-codes` (permission `Accounting.TaxCode.Create`) SHALL create a new tax code, rejecting a duplicate `Code` with `TaxCodeDuplicateException`.

#### Scenario: Creating a tax code with a duplicate code is rejected
- **WHEN** a tax code is created with a `Code` that already exists
- **THEN** the request is rejected with `TaxCodeDuplicateException`: "A tax code with code '{code}' already exists."

### Requirement: SST is calculated via a dedicated calculator service, exposed through the API
`POST /api/v1/accounting/tax/calculate-sst` (permission `Accounting.TaxCode.Read`) SHALL accept a sub-total and a `taxCodeId`, and return the tax amount and total computed via `ISstCalculator`, which SHALL delegate to `TaxCode.Calculate()`'s existing 2dp away-from-zero rounding rule rather than reimplementing it.

#### Scenario: Calculating SST for an unknown tax code is rejected
- **WHEN** `calculate-sst` is called with a `taxCodeId` that doesn't exist
- **THEN** the request is rejected with `NotFoundException`

#### Scenario: Calculator result matches the entity's own rounding
- **WHEN** `calculate-sst` is called with subTotal RM 2.50 against a 1%-rate tax code
- **THEN** the SST amount returned is RM 0.03, matching `TaxCode.Calculate(2.50m)` directly (away-from-zero rounding of the exact RM 0.025 midpoint)
