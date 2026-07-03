## 1. Backend

- [x] 1.1 Add `ISstCalculator`/`SstCalculator` (delegates to `TaxCode.Calculate()`)
- [x] 1.2 Add `TaxCodeDuplicateException`, wire to 409 in `ExceptionHandlingMiddleware`
- [x] 1.3 Add `GetTaxCodesQuery`/Handler, `CreateTaxCodeCommand`/Handler/Validator, `CalculateSstQuery`/Handler
- [x] 1.4 Add `TaxCodesController` (`GET/POST tax-codes`, `POST tax/calculate-sst`)
- [x] 1.5 Register `ISstCalculator` in `Application/DependencyInjection.cs`

## 2. Frontend

- [x] 2.1 Add `helpers/api/taxCodes.ts`
- [x] 2.2 Add `pages/accounting/TaxCodes.tsx` (list + create modal + inline calculator)
- [x] 2.3 Add route `/accounting/tax-codes` + menu item

## 3. Tests

- [x] 3.1 `SstCalculatorTests.cs` — delegation correctness, zero-rate case

## 4. Documentation

- [x] 4.1 OpenSpec accounting spec (this change)
- [x] 4.2 `docs/phase-1-accounting/Features.md` — check off SST Calculation
- [x] 4.3 `docs/phase-1-accounting/User_Guide.md` — new scenario

## 5. Verification

- [x] 5.1 Backend + frontend build clean, full test suite passes
- [x] 5.2 Live: create a tax code, use the inline calculator, confirm RM 2.50 at 1% → RM 0.03 (away-from-zero, not banker's rounding)
- [x] 5.3 Confirm Viewer gets 403 creating a tax code, 200 reading the list
