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
├── frontend/
│   ├── src/
│   │   ├── modules/
│   │   │   ├── accounting/
│   │   │   ├── sales/
│   │   │   ├── inventory/
│   │   │   └── admin/
│   │   ├── shared/                     # ui components, hooks, api client
│   │   ├── stores/                     # Zustand stores
│   │   ├── routes/
│   │   └── main.tsx
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

```
frontend/src/
  modules/accounting/
    pages/{ChartOfAccountsPage,GLEntriesPage,TrialBalancePage,BankReconciliationPage}.tsx
    components/{GLEntryForm,AccountPicker,ReconciliationTable}.tsx
    api/accountingApi.ts        # axios calls, typed
    store/accountingStore.ts    # Zustand slice
  modules/sales/
    pages/{CustomersPage,InvoicesPage,InvoiceDetailPage,PaymentsPage}.tsx
    components/{InvoiceForm,InvoiceLineEditor,PaymentModal}.tsx
    api/salesApi.ts
    store/salesStore.ts
  modules/inventory/  (same pattern)
  modules/admin/
    pages/{UsersPage,RolesPage,AuditLogsPage,FeatureFlagsPage}.tsx
  shared/
    components/{DataTable,Modal,ConfirmDialog,Toast,FormField,LoadingSpinner}.tsx
    hooks/{useFeatureFlags,usePermission,usePagination}.ts
    api/apiClient.ts            # axios instance, interceptors for JWT + 401 refresh
    utils/{currency.ts (MYR formatting), date.ts (Day.js wrappers)}
  stores/authStore.ts            # Zustand: user, tokens, roles, permissions, enabledModules
  routes/{AppRoutes.tsx, ProtectedRoute.tsx, ModuleGuard.tsx}
  main.tsx
```

## 4.4 File Naming Conventions

- **Backend:** PascalCase for files/classes matching the type name (`CreateInvoiceCommand.cs`). One public type per file.
- **Frontend:** PascalCase for components (`InvoiceForm.tsx`), camelCase for hooks/utils (`useFeatureFlags.ts`), kebab-case for non-component assets.
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
