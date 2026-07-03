# LitXus Systems — Documentation Update Policy

This project uses three documentation layers. Treat them as mandatory development outputs, not optional extras.

## 1. OpenSpec — system specification

**Where:** `openspec/specs/<capability>/spec.md` (source of truth for behavior), managed through `openspec/changes/` proposals. Config/project context: `openspec/config.yaml`.

**Covers:** business rules, workflows, module design, database structure, architecture, coding standards, system behavior.

**Rule:**
- Always read the relevant `openspec/specs/<capability>/spec.md` first, before implementing any feature that touches that capability.
- When business logic, a workflow, the architecture, or module behavior changes, update OpenSpec via a change proposal: `openspec new change <name>`, fill in `proposal.md`/`design.md`/`specs/`/`tasks.md` (see `openspec/config.yaml` for house rules), then `openspec archive <name>` once done.

## 2. OpenAPI — REST API contract

**Where:** Swashbuckle-generated (`backend/src/LitXus.Api/Program.cs`), Swagger UI in Development, hand-written reference at `docs/03_API_Specification.md`.

**Covers:** endpoints, request/response models, authentication, API documentation.

**Rule:**
- Only update OpenAPI-related docs when the API contract itself changes — adding, removing, or modifying endpoints, request/response schemas, or auth.
- Do not modify OpenAPI documentation for internal logic changes that don't affect API behavior.

## 3. User Guides — real-world usage

**Where:** `docs/phase-N-<name>/User_Guide.md` (see `docs/phase-1-accounting/User_Guide.md` for the format to follow).

**Covers:** real business scenarios, step-by-step instructions, sample data inputs, expected results, accounting/system impact, a verification checklist.

**Rule:**
- Every implemented feature or workflow must include or update at least one User Guide.
- Guides must reflect real-world usage (what a user does and sees), not technical/internal descriptions.

## 4. Development Completion Rule

A feature is only complete when **all applicable** items are updated:
- Code implementation
- OpenSpec (if business logic/workflow/architecture/module behavior changed)
- OpenAPI (if the API contract changed)
- User Guide (created or updated with a real scenario + sample data)

If any applicable item is missing, the task is not done.

## 5. Priority order

1. Read OpenSpec (understand current system behavior)
2. Implement the code
3. Update OpenSpec (if needed)
4. Update OpenAPI (if needed)
5. Create or update the User Guide

## 6. Neither applies

If a change affects neither business logic/architecture nor the REST contract, don't modify OpenSpec or OpenAPI unnecessarily — but still consider whether a User Guide is warranted.

## 7. Post-Task Documentation Checklist (mandatory)

Before considering any task complete, run this checklist:

**OpenSpec** — did this task change business logic, workflows, module behavior, architecture/design, or introduce new business rules? If yes, update the relevant `openspec/specs/<capability>/spec.md`.

**OpenAPI** — did this task add/modify/remove an API endpoint, change a request/response model, or change auth behavior? If yes, update the OpenAPI-facing docs (`docs/03_API_Specification.md`). If no, don't touch it.

**User Guide** — does this feature need user instructions, or does an existing guide need updating? If yes, create/update a guide with: Objective, Prerequisites, a real business scenario, sample data, step-by-step instructions, expected results, accounting/system impact (if applicable), and a verification checklist.

**Final verification** before closing any task:
- ✅ Code implementation complete
- ✅ OpenSpec updated if required
- ✅ OpenAPI updated if required
- ✅ User Guide created/updated if required
- ✅ Documentation matches the final implementation
- ✅ Assumptions, limitations, or TODOs are documented

**End every completed task with a Documentation Summary:**
```
- OpenSpec: Updated / Not Required (reason)
- OpenAPI: Updated / Not Required (reason)
- User Guide: Created / Updated / Not Required (reason)
- Outstanding TODOs: <list, or "None">
```

## Other references

- `docs/00_Overview.md` — human-readable planning docs index (docs/00-17), still the canonical reference; `openspec/` is a separate, machine-readable layer alongside it, not a replacement.
- `openspec/config.yaml` — project context and per-artifact rules for OpenSpec change proposals.
