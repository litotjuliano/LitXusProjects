## Why

`TaxCode.Calculate()` existed on the domain entity (correct 2dp away-from-zero rounding, fully unit-tested) and 2 tax codes were seeded (SST-6, SST-0), but nothing in the Application/API layer ever called it — GL entries hardcoded `subtotal * 0.06m` directly instead of looking up a `TaxCode`, and there was no way to manage tax codes or calculate SST through the API at all. `docs/15_Malaysia_Compliance.md` §15.1 and `docs/phase-1-accounting/API_Specification.md` already specified the exact contract this change implements.

## What Changes

- New `ISstCalculator` (Application) delegating to the already-tested `TaxCode.Calculate()`.
- New CQRS: list tax codes, create a tax code (rejecting duplicate codes), calculate SST for a given subtotal + tax code.
- New `TaxCodesController`: `GET/POST /api/v1/accounting/tax-codes`, `POST /api/v1/accounting/tax/calculate-sst`.
- New frontend `Tax Codes` page (list, create, and an inline calculator widget so the calculate endpoint is actually exercised from the UI).

## Capabilities

### Modified Capabilities
- `accounting`: adds tax-code management and the SST calculator service as requirements — the rounding rule itself was already spec'd (baseline), this adds the API/service surface around it.

### New Capabilities
None.

## Impact

- Backend: `ISstCalculator`/`SstCalculator`, `TaxCodeDuplicateException`, `GetTaxCodesQuery`, `CreateTaxCodeCommand`/Validator, `CalculateSstQuery`, `TaxCodesController`, `TaxCodeDto`/`SstCalculationDto`.
- Frontend: `helpers/api/taxCodes.ts`, `pages/accounting/TaxCodes.tsx`, new route + menu item.
- Tests: `SstCalculatorTests.cs` (delegation correctness).
- Docs: `docs/phase-1-accounting/Features.md` (check off SST Calculation), `docs/phase-1-accounting/User_Guide.md` (new scenario).
- No database schema changes — `TaxCodes` table already existed; no new FK is added anywhere in Phase 1 (a future `InvoiceLines.TaxCodeId` is Phase 2 scope, not touched here).
