# 12 — Documentation Templates

## 12.1 User Guide Structure

```
1. Getting Started (login, navigating the dashboard)
2. {Module} — for each licensed module:
   2.1 Overview & key concepts
   2.2 Step-by-step: {core task, e.g. "Creating and posting a GL entry"} — numbered steps + screenshot per step
   2.3 Common tasks (search/filter, export, edit, deactivate)
   2.4 FAQ / troubleshooting for this module
3. Reports — how to generate, filter, export each report
4. Account & Settings — profile, password change, notification preferences
5. Glossary (Malaysia-specific terms: SST, LHDN, Sdn Bhd, etc.)
```
Screenshots mandatory at every numbered step; short screen-recording clips optional per module (Phase 5 stretch goal).

## 12.2 Developer Guide Structure

```
1. Architecture overview (link to 01_Architecture.md)
2. Local setup (link to 10_Deployment.md §10.1)
3. Code organization — where to add a new: entity / command / query / endpoint / React page
4. Coding conventions (naming, async/await everywhere, ViewModel/DTO-never-domain-model rule)
5. How to add a new report (worked example end-to-end: query handler -> DTO -> controller -> React page)
6. How to add a new permission (enum -> seeder -> attribute usage -> frontend usePermission check)
7. Running tests (unit, integration, frontend)
8. Database migrations — creating, applying, rolling back
9. Contribution workflow (branching, PR, CI gate)
```

## 12.3 API Documentation

Auto-generated Swagger UI is the primary reference (always current, since it's generated from code). This doc type is just the *pointer*: where Swagger is hosted per environment (`/swagger` locally and in Demo; disabled or admin-gated in Production), and how to export/import the OpenAPI spec into Postman for manual testing.

## 12.4 Deployment Guide Structure

```
1. Prerequisites (per hosting option — link to 10_Deployment.md)
2. Option A: Self-Hosted IIS — step-by-step
3. Option B: Customer Cloud (AWS/Azure) — step-by-step
4. Option C: Managed Service — what LitXus handles vs. customer
5. Docker quick-start (docker-compose up)
6. Configuration reference (every appsettings key, what it does, default value)
7. Database backup & restore procedure
8. Upgrading to a new version (migration application order, rollback plan)
9. Troubleshooting (link to 10_Deployment.md §10.9, expanded per real issues hit)
```

## 12.5 Testing Report Template

```markdown
# Phase N Testing Report

## Summary
Unit tests: {count} passed / {count} total ({coverage}% Domain+Application coverage)
Integration tests: {count} passed / {count} total
Manual scenarios: {count} / {count} verified

## Results by Category
| Category | Total | Passed | Failed | Notes |
|---|---|---|---|---|
| Happy path | | | | |
| Error cases | | | | |
| Edge cases | | | | |
| Security | | | | |

## Known Issues
{list, with severity and workaround if any}

## Sign-off
Tested by: ___  Date: ___  Approved by: ___
```

## 12.6 Phase Completion Report Template

```markdown
# Phase N Completion Report — {Module Name}

**Duration:** {planned} vs {actual}
**Scope delivered:** {checklist against Features.md acceptance criteria}
**Deviations from OpenSpec:** {any, with justification}
**Test results:** link to Testing Report
**Documentation delivered:** {checklist: Swagger updated / user guide / dev guide}
**Known issues carried forward:** {list}
**Sign-off:** {who, date}
**Next phase readiness:** {any blockers for Phase N+1}
```

## 12.7 Release Notes Template

```markdown
# LitXus Systems vX.Y — Release Notes ({date})

## New Features
- {feature, plain language, per product it applies to}

## Improvements
- {item}

## Bug Fixes
- {item}

## Known Issues
- {item, with workaround}

## Upgrade Notes
- {migration steps required, breaking changes if any}
```

## 12.8 Known Issues Template

```markdown
| ID | Description | Severity | Module | Workaround | Target Fix Version |
|---|---|---|---|---|---|
```
