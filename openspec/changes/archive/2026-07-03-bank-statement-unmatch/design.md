## Context

Flagged as a known gap in the bank-reconciliation change's own design.md ("Risks / Trade-offs — no Unmatch action... flagged here for a future change if it comes up in practice"). This is that future change.

## Decisions

- **`Unmatch()` mirrors `Match()`'s exact guard shape** — throws a dedicated `DomainException` (`StatementLineNotMatchedException`) rather than silently no-op'ing, consistent with how `Match()` itself rejects re-matching rather than silently succeeding.
- **No cross-entity validation needed in the handler** (unlike `MatchBankStatementLineCommandHandler`) — unmatching only touches the statement line itself; there's no "is the GL line still eligible" question the way there was for matching.
- **Single endpoint (`.../unmatch`), no request body** — nothing to pass beyond the statement line's own id.

## Risks / Trade-offs

None beyond what `Match()` already carries — this is the direct inverse operation.
