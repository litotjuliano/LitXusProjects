# Phase 1 — UI Mockups

Design language: Tailwind CSS, clean/professional (financial-software tone, not consumer-flashy). Every list screen: loading skeleton state, empty state with a call-to-action, error state with retry, populated state with pagination.

## Screen: Login

```
┌─────────────────────────────────────┐
│           LitXus Systems              │
│         Accounting Pro                │
│                                       │
│  Email     [______________________]  │
│  Password  [______________________]  │
│                                       │
│           [    Log In    ]           │
│           Forgot password?           │
└─────────────────────────────────────┘
```

## Screen: Dashboard (post-login shell)

```
┌───────────┬───────────────────────────────────────────┐
│ LitXus    │  Dashboard                                  │
│           │  ┌────────┐ ┌────────┐ ┌────────┐          │
│ Dashboard │  │ Cash   │ │ Draft  │ │ Unbalnc│          │
│ Chart of  │  │ RM 42k │ │ GL: 3  │ │  ed: 0 │          │
│  Accounts │  └────────┘ └────────┘ └────────┘          │
│ GL Entries│                                             │
│ Bank      │  Recent GL Entries                          │
│  Recon    │  ┌───────────────────────────────────────┐ │
│ Reports   │  │ JE-2026-000045  Posted  Jul 1  RM3,500 │ │
│ Admin >   │  │ ...                                     │ │
│           │  └───────────────────────────────────────┘ │
└───────────┴───────────────────────────────────────────┘
```
Nav items are conditionally rendered per `usePermission`/`enabledModules` — "Admin" section only visible to Admin role.

## Screen: Chart of Accounts

```
┌──────────────────────────────────────────────────┐
│ Chart of Accounts                    [+ New Account]│
│ [Search...] [Type: All ▾] [ ] Show inactive         │
│                                                      │
│ ▾ 1000 Assets                                       │
│   1010  Cash                              RM 42,000  │
│   1020  Accounts Receivable               RM 18,500  │
│ ▾ 2000 Liabilities                                  │
│   2100  Accrued Liabilities                RM 3,500  │
│ ...                                                  │
└──────────────────────────────────────────────────┘
```
Create/Edit via modal: Code, Name, Type (dropdown), Parent Account (searchable picker). Client + server validation; error messages inline under each field per the CRUD convention in the top-level CLAUDE.md-style repo conventions (server validation is authoritative).

## Screen: GL Entries List

```
┌────────────────────────────────────────────────────────┐
│ GL Entries                              [+ New Entry]     │
│ [Status: All ▾] [Date range] [Account ▾]  [Export ▾]      │
│                                                            │
│ Number          Date    Status  Description       Amount  │
│ JE-2026-000045  Jul 1   Posted  July rent accrual  3,500  │
│ (Draft)         Jul 1   Draft   Office supplies      120  │
│ JE-2026-000012  Jun 28  Voided  Duplicate entry      850  │
│                                                            │
│                            « 1 2 3 ... 6 »                │
└────────────────────────────────────────────────────────┘
```

## Screen: GL Entry Form (Create/Edit)

```
┌────────────────────────────────────────────────────────┐
│ New GL Entry                                              │
│ Entry Date  [2026-07-01]     Description [____________]   │
│                                                            │
│ Account              Description        Debit    Credit   │
│ [5100 Rent Exp   ▾] [Rent - Shah Alam] [3,500.00] [     ] │
│ [2100 Accrued Li ▾] [               ] [       ] [3,500.00]│
│ [+ Add Line]                                              │
│                                                            │
│ Total:                             3,500.00   3,500.00    │
│ ✓ Balanced                                                │
│                                                            │
│         [Save as Draft]   [Save & Post]                   │
└────────────────────────────────────────────────────────┘
```
Running balance check shown live client-side (UX convenience); server re-validates on submit regardless. "Save & Post" disabled unless balanced and ≥2 lines.

## Screen: Bank Reconciliation

```
┌───────────────────────────┬───────────────────────────┐
│ Bank Statement Lines        │ Unmatched GL Lines          │
│ (Maybank ****1234)          │                             │
│                              │                             │
│ Jul 1  -RM 1,200  Rent       │ Jul 1  Dr RM 1,200  Rent    │
│ Jul 3  +RM 5,000  Customer   │ Jul 3  Cr RM 5,000  AR pmt  │
│                              │                             │
│ [Select line] [Select line]  │                             │
│              [Match Selected]│                             │
└───────────────────────────┴───────────────────────────┘
Reconciliation status: 8 of 10 lines matched
```

## Screen: Reports (Trial Balance example)

```
┌────────────────────────────────────────────────────────┐
│ Trial Balance          As of [2026-07-01]  [Export ▾]     │
│                                                            │
│ Account                        Debit          Credit      │
│ 1010 Cash                    42,000.00                    │
│ 1020 Accounts Receivable      18,500.00                    │
│ 2100 Accrued Liabilities                     3,500.00      │
│ 4000 Revenue                                57,000.00      │
│ ...                                                        │
│ TOTAL                        60,500.00      60,500.00      │
│                              ✓ Balanced                    │
└────────────────────────────────────────────────────────┘
```

## Screen: Audit Log Viewer (Admin)

```
┌────────────────────────────────────────────────────────┐
│ Audit Logs                                                │
│ [Entity ▾] [User ▾] [Date range] [Action ▾]               │
│                                                            │
│ Jul 1 09:00  accountant@litxus.demo  Approve  GLEntry      │
│   Before: { status: "Draft" }                              │
│   After:  { status: "Posted" }                              │
│ Jul 1 08:55  accountant@litxus.demo  Create   GLEntry      │
│ ...                                                        │
└────────────────────────────────────────────────────────┘
```

## Primary User Flow — Create, Post, and Reconcile a GL Entry

1. Accountant logs in → lands on Dashboard.
2. Navigates to GL Entries → "New Entry".
3. Fills header (date, description), adds 2+ lines, sees live balance indicator turn green.
4. Clicks "Save & Post" → toast "Entry JE-2026-000045 posted" → redirected to entry detail (read-only view now).
5. Later, navigates to Bank Reconciliation → imports statement CSV → matches the new bank line to the GL entry's cash line.
6. Views Trial Balance → confirms the new entry's impact is reflected and the report still balances.
