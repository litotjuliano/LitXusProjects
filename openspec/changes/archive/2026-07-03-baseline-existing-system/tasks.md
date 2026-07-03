## 1. Research

- [x] 1.1 Inventory existing Domain modules, entities, migrations, controllers, and frontend pages
- [x] 1.2 Extract enforced business rules for the Accounting module (GL balancing, account rules, tax rounding, bank reconciliation, report calculations) directly from entity/validator/handler code
- [x] 1.3 Extract enforced rules for Identity/RBAC (Super Admin protections, JWT claims/lifetimes, permission/module gating, login checks) directly from handler/service code
- [x] 1.4 Extract enforced rules for Audit Trail (interceptor capture, explicit semantic logging, query filters) directly from interceptor/service/controller code
- [x] 1.5 Confirm Company Profile validation rules directly from FluentValidation validators

## 2. Capability specs

- [x] 2.1 Write `specs/identity-auth/spec.md`
- [x] 2.2 Write `specs/accounting/spec.md`
- [x] 2.3 Write `specs/company-profile/spec.md`
- [x] 2.4 Write `specs/licensing/spec.md`
- [x] 2.5 Write `specs/audit-trail/spec.md`

## 3. Validation

- [x] 3.1 Run `openspec validate --change baseline-existing-system --strict` and fix any schema errors
- [x] 3.2 Spot-check specs against source (licensing rejection-message text, identity-auth Super Admin guard text) to confirm no invented behavior
- [x] 3.3 Confirm zero changes to OpenAPI/Swagger files, controllers, or `docs/00-17` as a result of this change
