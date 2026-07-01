# 15 — Malaysia Compliance Implementation Guide

## 15.1 SST (Sales & Service Tax) @ 6%

- `TaxCodes` table ([02_Database_Schema.md](02_Database_Schema.md) §2.2) holds rate as data, not a hardcoded constant — rate changes (as have happened historically) require a config/data change, not a code deploy.
- Calculation centralized in a single `ISstCalculator` service (Application layer), called from both Invoice creation (Sales) and any manual tax entry (Accounting) — one place to test, one place to fix if the formula needs adjustment.

```csharp
public class SstCalculator : ISstCalculator
{
    public (decimal SstAmount, decimal Total) Calculate(decimal subTotal, TaxCode taxCode)
    {
        var sst = Math.Round(subTotal * (taxCode.Rate / 100m), 2, MidpointRounding.AwayFromZero);
        return (sst, subTotal + sst);
    }
}
```
- Rounding rule (`AwayFromZero` at 2dp) documented explicitly since tax rounding disputes are a common audit friction point.

## 15.2 LHDN MyInvois (E-Invoicing Readiness)

- v1.0 scope: **readiness, not full integration** (per the locked prompt's "E-invoicing readiness" wording, not "E-invoicing implemented").
- Concretely: `Invoices` schema already captures everything MyInvois requires (buyer/seller TIN fields to be added as nullable columns in a Phase 5 migration, structured line items, tax breakdown) so that a future MyInvois API integration is additive, not a schema rework.
- `Invoice` domain entity exposes a `ToMyInvoisPayload()` mapping stub (returns the UBL/JSON shape MyInvois expects) — implemented as a documented no-op/TODO in v1.0, ready to wire to the actual LHDN API in v1.1 once the customer's MyInvois onboarding (a business process, not a code task) is complete.
- Document numbering (§15.6) is a MyInvois prerequisite already fully implemented in v1.0 regardless.

## 15.3 Bank Negara Data Residency

- Deployment options (Option 1 Self-Hosted, Option 3 Managed Service in Malaysia-region cloud — [10_Deployment.md](10_Deployment.md) §10.3) let a customer keep data within Malaysia if their Bank Negara guidelines or internal policy requires it.
- No data ever leaves the customer's chosen deployment boundary by default — no telemetry/analytics phone-home to a LitXus-operated service unless the customer opts into the Managed Service option.

## 15.4 Companies Act 2016 Compliance

- Chart of Accounts structure and required financial statements (Balance Sheet, Income Statement — [03_API_Specification.md](03_API_Specification.md) §3.5) map to statutory reporting formats required for annual filing.
- Audit trail ([07_Audit_Trail.md](07_Audit_Trail.md)) satisfies the "proper accounting records" requirement (records must show/explain transactions, be retained, and support true-and-fair financial statements).
- Customer entity types seeded include Sdn Bhd (private limited) reflecting the Companies Act 2016 entity structure customers will actually operate under.

## 15.5 PDPA (Personal Data Protection Act) Implementation

- Personal data collected: user profile (name, email, phone), customer contact details. Minimized to what's operationally needed — no unnecessary personal data fields.
- Access control (RBAC, [06_RBAC_Auth.md](06_RBAC_Auth.md)) satisfies the security-safeguard principle — personal data is permission-gated, not open to every authenticated user.
- Audit logging of who accessed/exported personal-data-containing reports ([07_Audit_Trail.md](07_Audit_Trail.md) §7.7) satisfies the accountability principle.
- Data retention: audit logs 7 years (regulatory minimum); operational data (customers, invoices) retained indefinitely unless the customer explicitly deactivates/archives — no automatic PII deletion that would break financial record-keeping obligations, since Companies Act retention requirements and PDPA data-minimization need to be balanced, not treated as contradictory.
- No cross-border data transfer by default (§15.3) — relevant since PDPA restricts transfers outside Malaysia without safeguards.

## 15.6 Document Numbering Controls

- Invoice numbers and GL entry numbers are sequential and gap-free by design (enforced via a DB sequence or serializable transaction — see [13_Roadmap.md](13_Roadmap.md) risk register), satisfying both LHDN and Companies Act expectations that financial documents are traceable and non-skippable.
- Voided documents keep their number (never reused) — a void is a status change with an audit trail, not a deletion that would create a numbering gap.

## 15.7 Audit Trail Requirements (7-Year)

Fully covered in [07_Audit_Trail.md](07_Audit_Trail.md) §7.5 — restated here as the compliance anchor: 7-year minimum retention, immutable rows, before/after values captured for every financial record change.

## 15.8 Compliance Validation Checklist (Phase 5)

- [ ] SST calculation verified against LHDN's published rate and rounding guidance
- [ ] Invoice/GL numbering confirmed gap-free under concurrent-load testing
- [ ] Audit log immutability confirmed at the DB permission level (attempt an UPDATE with the app's SQL login, confirm it's rejected)
- [ ] PDPA data-minimization review: confirm no unused personal data fields exist in the schema
- [ ] Data residency documented per deployment option in the deployment guide
- [ ] MyInvois-readiness fields present in schema, mapping stub in place, explicitly flagged as "not yet wired to LHDN API" in release notes so it's not mistaken for a completed integration
