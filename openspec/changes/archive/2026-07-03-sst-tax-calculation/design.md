## Context

`docs/15_Malaysia_Compliance.md` §15.1 already specified `ISstCalculator` as a single-source-of-truth Application-layer service, and `docs/phase-1-accounting/API_Specification.md` already gave the exact endpoint contract (`GET/POST /accounting/tax-codes`, `POST /accounting/tax/calculate-sst`). This change implements that existing spec, not new design — the only real decision was how to implement the calculator without duplicating logic.

## Goals / Non-Goals

**Goals:**
- Expose tax-code management and SST calculation through the API, per the already-documented contract.
- Give the calculator a real UI consumer (not just a dangling endpoint for a future Sales-module phase).

**Non-Goals:**
- Wiring the calculator into GL entry posting/validation — not specified anywhere in `Business_Rules.md`, which explicitly scopes the calculator's Phase 1 callers to the standalone endpoint only ("in later phases" for Sales invoice lines).
- TaxCode update/delete — no `Accounting.TaxCode.Update`/`Delete` permission exists in the catalog; Phase 1 scope is list + create only.

## Decisions

- **`SstCalculator.Calculate()` delegates to `TaxCode.Calculate()`** rather than reimplementing the rounding formula (the compliance doc's code sample duplicates it) — one rounding implementation, already unit-tested on the entity itself.
- **`CalculateSstQuery` is a Query, not a Command** — it's a pure computation with no persistence side effects, consistent with how reports are modeled elsewhere in this codebase.
- **Duplicate-code rejection mirrors `AccountCodeDuplicateException`/`CreateAccountCommandHandler` exactly** (same shape, same 409 mapping) rather than inventing a different pattern for a structurally identical problem.

## Risks / Trade-offs

- [The `/tax/calculate-sst` endpoint has no consumer yet besides the new admin UI widget — Sales invoices, its real intended caller, don't exist until Phase 2] → Mitigation: the endpoint contract is already fixed in `docs/03_API_Specification.md`, so Phase 2 can consume it without a breaking change; the inline calculator widget keeps it from being genuinely untested/unused code in the meantime.
