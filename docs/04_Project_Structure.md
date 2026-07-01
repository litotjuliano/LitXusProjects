# 04 — Project Structure

## 4.1 Monorepo Layout

```
LitXus-Systems/
├── backend/
│   ├── src/
│   │   ├── LitXus.Api/                 # Presentation layer
│   │   ├── LitXus.Application/          # CQRS handlers, DTOs, validators
│   │   ├── LitXus.Domain/               # Entities, business rules
│   │   └── LitXus.Infrastructure/       # EF Core, Identity, repos
│   ├── tests/
│   │   ├── LitXus.UnitTests/
│   │   └── LitXus.IntegrationTests/
│   ├── LitXus.sln
│   └── Directory.Build.props
├── frontend/                            # Konrix Envato template (used as-is) + LitXus pages
│   ├── src/
│   │   ├── pages/{auth,accounting,admin,apps,ui,...}/   # accounting/ + admin/ are LitXus's own
│   │   ├── components/                 # template's shared UI kit (VerticalForm, Modal, etc.)
│   │   ├── helpers/api/                # apiCore, auth.ts, accounting.ts
│   │   ├── redux/{auth,layout}/, store.ts
│   │   ├── constants/menu.ts
│   │   ├── routes/
│   │   └── index.tsx / App.tsx
│   ├── public/
│   ├── vite.config.ts
│   └── package.json
├── docs/
│   ├── 00_Overview.md ... 16_Feature_Flags.md   (this planning set)
│   ├── openapi/openapi.json
│   ├── phase-1-accounting/
│   │   ├── Features.md
│   │   ├── API_Specification.md
│   │   ├── Database_Schema.md
│   │   ├── Business_Rules.md
│   │   ├── UI_Mockups.md
│   │   ├── Test_Scenarios.md
│   │   └── Sample_Data.md
│   ├── phase-2-sales/  (same structure)
│   ├── phase-3-inventory/
│   ├── phase-4-integration/
│   └── phase-5-release/
├── .github/workflows/ci.yml
├── docker-compose.yml
└── README.md
```

## 4.2 Backend Project Structure (detail)

```
LitXus.Domain/
  Modules/Accounting/Entities/{Account,GLEntry,GLEntryLine,TaxCode,BankAccount,BankStatementLine}.cs
  Modules/Sales/Entities/{Customer,Invoice,InvoiceLine,Payment,CreditNote}.cs
  Modules/Inventory/Entities/{Product,Warehouse,StockLevel,StockMovement,StockValuationLayer}.cs
  Modules/Shared/Entities/{AuditLog,Notification,License}.cs
  Modules/Identity/Entities/{Role,Permission,RolePermission}.cs
  Common/{BaseEntity,IAuditable,DomainException}.cs
  Events/{InvoicePostedEvent,StockMovementRecordedEvent}.cs

LitXus.Application/
  Modules/Accounting/Commands/{CreateGLEntry,PostGLEntry,VoidGLEntry}/{Command,Handler,Validator}.cs
  Modules/Accounting/Queries/{GetTrialBalance,GetIncomeStatement,...}/{Query,Handler}.cs
  Modules/Sales/Commands/{CreateInvoice,IssueInvoice,RecordPayment,VerifyPayment}/...
  Modules/Inventory/Commands/{CreateProduct,RecordStockMovement}/...
  Common/Behaviors/{ValidationBehavior,LoggingBehavior,TransactionBehavior}.cs
  Common/Interfaces/{IAppDbContext,ICurrentUserService,IFeatureFlagService,IDateTimeProvider}.cs
  Mappings/{AccountingProfile,SalesProfile,InventoryProfile}.cs

LitXus.Infrastructure/
  Persistence/AppDbContext.cs
  Persistence/Configurations/{AccountConfiguration,InvoiceConfiguration,...}.cs   (EF Fluent API, one per entity)
  Persistence/Migrations/
  Persistence/Interceptors/AuditSaveChangesInterceptor.cs
  Identity/{IdentityService,JwtTokenGenerator,CurrentUserService}.cs
  Seeding/{Phase1AccountingSeeder,Phase2SalesSeeder,Phase3InventorySeeder}.cs
  DependencyInjection.cs

LitXus.Api/
  Controllers/{AuthController,AccountingController,GLEntriesController,SalesInvoicesController,
               PaymentsController,InventoryProductsController,AdminUsersController,AuditLogsController}.cs
  Middleware/{ExceptionHandlingMiddleware,RequestLoggingMiddleware}.cs
  Filters/{RequireModuleAttribute,RequirePermissionAttribute}.cs
  Program.cs
  appsettings.json / appsettings.Development.json / appsettings.Production.json
```

## 4.3 Frontend Project Structure (detail)

The frontend is **not** built from a bare Vite scaffold — it is the [Konrix](https://coderthemes.com) Envato React/Tailwind admin template, used as-is per the locked spec, with LitXus's own pages/API wiring layered on top of the template's existing layout, auth, and Redux plumbing. Source: `/Users/litojuliano/LitXus Documentations/Konrix_React/Admin`, copied into `frontend/` and customized (see [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.5 for the exact diff).

```
frontend/src/
  pages/
    auth/{Login,Register,RecoverPassword,LockScreen}.tsx      # template's own, wired to /auth/* endpoints
    accounting/                                                 # LitXus Phase 1 pages (new)
      Dashboard.tsx
      ChartOfAccounts.tsx
      GLEntries.tsx
      BankReconciliation.tsx
      reports/{TrialBalance,IncomeStatement,BalanceSheet,GeneralLedger}.tsx
    admin/{Users,Roles,AuditLogs}.tsx                           # LitXus admin pages (new)
    apps/, extra/, error/, ui/, extended/, forms/, tables/      # Konrix demo/showcase pages,
                                                                 # left in place ("use as-is") but
                                                                 # NOT linked from the LitXus nav —
                                                                 # candidates for deletion in Phase 5 polish
  components/                                                   # template's shared UI kit — VerticalForm,
                                                                 # FormInput, AuthLayout, PageBreadcrumb,
                                                                 # HeadlessUI/ModalLayout, etc. — reused as-is
  helpers/api/
    apiCore.ts               # axios wrapper, session storage, Bearer auth header (template had "JWT ", fixed)
    auth.ts                  # login/logout/register/forgot-password — repointed to /auth/* per API spec
    accounting.ts             # LitXus: accounts + GL entries calls (new, Phase 1)
  redux/
    auth/{actions,constants,reducers,saga}.ts   # template's auth slice — repointed from username to email,
                                                 # response envelope adapted to { data: {...} } shape
    layout/                   # template's own (sidebar collapse, theme, etc.) — untouched
    store.ts
  constants/menu.ts            # LitXus nav (Dashboard, Accounting, Administration) replacing the
                                # template's demo-app menu; Sales/Inventory sections added in Phase 2/3
  routes/{Routes,index,PrivateRoute}.tsx   # template's routing — accountingRoutes/adminRoutes added
  layouts/{Vertical,Default,LeftSideBar,Topbar,Menu,Footer}.tsx  # template's shell, untouched
  config.ts                    # fixed to read import.meta.env.VITE_API_URL (template had process.env,
                                # a leftover from a CRA port that doesn't work under Vite)
  index.tsx / App.tsx
```

**What was changed vs. the stock template** (full list in [14_Tech_Implementation.md](14_Tech_Implementation.md) §14.5): `config.ts` env var source, `apiCore.ts` auth header prefix (`JWT` → `Bearer`) and session key name, `helpers/api/auth.ts` endpoint paths and `username`→`email` field, `redux/auth/{actions,saga,reducers}.ts` field names and response envelope, `Login.tsx`/`Register.tsx`/`RecoverPassword.tsx` field names and removed social-login UI, `constants/menu.ts` full replacement, `index.html`/`PageBreadcrumb.tsx` branding, `package.json` name/build script (decoupled `tsc` from `vite build` — see §14.5 for why).

## 4.4 File Naming Conventions

- **Backend:** PascalCase for files/classes matching the type name (`CreateInvoiceCommand.cs`). One public type per file.
- **Frontend:** follows the Konrix template's existing conventions — PascalCase for pages/components (`GLEntries.tsx`), camelCase for helpers (`accounting.ts` exporting named functions, not a class), Redux slices per domain folder under `redux/`.
- **Migrations:** `yyyyMMddHHmmss_PhaseN_Description` (EF Core default timestamp prefix retained).
- **OpenSpec docs:** fixed filenames per phase folder (`Features.md`, `API_Specification.md`, etc.) so tooling/scripts can rely on the path shape.

## 4.5 Git Branching Strategy

```
main         production releases only, tagged (v1.0-phase1, v1.0-phase2, ..., v1.0)
develop      integration branch, all phase work merges here first
feature/*    e.g. feature/phase1-gl-entries, feature/phase2-invoice-crud
             branched from develop, PR back into develop
hotfix/*     branched from main for urgent production fixes, merged to both main and develop
```

Merge to `main` only happens at phase-completion sign-off (per [05_Phase_Breakdown.md](05_Phase_Breakdown.md) success criteria), never mid-phase.
