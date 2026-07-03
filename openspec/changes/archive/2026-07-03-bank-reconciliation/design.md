## Context

The domain layer (`BankAccount`, `BankStatementLine.Match()`) and the API contract (`docs/phase-1-accounting/API_Specification.md`) already existed before this change; this implements them. The one genuinely new design decision was the CSV import format, which `docs/08_Sample_Data.md` had flagged as needing to be designed from scratch.

## Goals / Non-Goals

**Goals:**
- Implement the already-spec'd 6-endpoint contract exactly.
- Add the one missing endpoint (`unmatched-gl-lines`) needed to make the two-pane mockup screen actually functional, and document it since it wasn't in the original spec.
- Design a CSV import format that's simple enough to hand-parse safely (no new dependency) but still handles realistic bank-export quirks (a comma inside a description).

**Non-Goals:**
- An "Unmatch" endpoint — `Business_Rules.md`'s "matched at most once" rule only requires *rejecting* a re-match, not necessarily exposing an unmatch action; not in the original API spec, so not added speculatively.
- CSV library dependency — 3 fixed columns don't justify one, consistent with the earlier CSV-export decision in this same codebase.

## Decisions

- **CSV format: `Date,Description,Amount` header, `yyyy-MM-dd`, signed decimal amount** (+deposit/-withdrawal, matching the existing `BankStatementLines.Amount` schema comment). Hand-rolled parser with RFC4180-style quoted-field support (so a comma inside a real bank description survives), living in Application (`BankStatementCsvParser`) since it throws the Application `ValidationException` other validation failures already use.
- **Import is all-or-nothing** — every row is validated before any row is persisted; a single bad row rejects the whole file with every bad row's error listed at once. Matches how the rest of this codebase rejects invalid financial input outright (e.g. an unbalanced GL entry) rather than partially applying it.
- **Match eligibility (Posted-only, no double-claiming a GL line) is enforced in the handler, not the domain entity** — `BankStatementLine.Match()` only knows about its own `IsReconciled` state; checking the *target* `GLEntryLine`'s status and whether another statement line already claims it requires cross-entity queries the handler already has `IAppDbContext` access for. New `GLEntryLineAlreadyMatchedException` (409) for the "already claimed elsewhere" case; an invalid/non-Posted `GLEntryLineId` reuses `NotFoundException` since the UI only ever offers Posted, unmatched lines as options — a caller hitting either of those paths is off the happy path the UI enforces.
- **`unmatched-gl-lines` query reuses `GetGeneralLedgerQueryHandler`'s join pattern** (`GLEntryLines` joined to `GLEntries`, filtered to `Posted`) rather than a new approach, plus an `AccountId` filter to the bank account's linked GL account and a `NOT IN (matched ids)` exclusion.

## Risks / Trade-offs

- [Hand-rolled CSV parser, not a battle-tested library] → Mitigation: format is deliberately minimal (3 fixed columns), fully unit-tested including the comma-in-quoted-field edge case and multiple malformed-row scenarios; same risk profile already accepted for CSV export elsewhere in this codebase.
- [No "Unmatch" action] → Mitigation: not in the original spec; a real gap if a wrong match needs correcting, but out of scope for this pass — flagged here for a future change if it comes up in practice.
