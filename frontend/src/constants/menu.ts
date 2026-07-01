export interface MenuItemTypes {
  key: string;
  label: string;
  isTitle?: boolean;
  icon?: string;
  url?: string;
  parentKey?: string;
  target?: string;
  children?: MenuItemTypes[];
}

// LitXus Accounting Pro (Phase 1) navigation. Sales/Inventory sections are added
// in Phase 2/3 and shown or hidden per the licensed EnabledModules (see
// docs/16_Feature_Flags.md) rather than being hardcoded here permanently.
const MENU_ITEMS: MenuItemTypes[] = [
  {
    key: 'menu',
    label: 'Menu',
    isTitle: true,
  },
  {
    key: 'dashboard',
    label: 'Dashboard',
    isTitle: false,
    icon: 'mgc_home_3_line',
    url: '/dashboard'
  },
  {
    key: 'accounting',
    label: 'Accounting',
    isTitle: true,
  },
  {
    key: 'accounting-chart-of-accounts',
    label: 'Chart of Accounts',
    isTitle: false,
    icon: 'mgc_list_check_line',
    url: '/accounting/chart-of-accounts',
  },
  {
    key: 'accounting-gl-entries',
    label: 'GL Entries',
    isTitle: false,
    icon: 'mgc_book_2_line',
    url: '/accounting/gl-entries',
  },
  {
    key: 'accounting-bank-reconciliation',
    label: 'Bank Reconciliation',
    isTitle: false,
    icon: 'mgc_bank_card_line',
    url: '/accounting/bank-reconciliation',
  },
  {
    key: 'accounting-reports',
    label: 'Reports',
    isTitle: false,
    icon: 'mgc_chart_bar_line',
    children: [
      {
        key: 'reports-trial-balance',
        label: 'Trial Balance',
        url: '/accounting/reports/trial-balance',
        parentKey: 'accounting-reports',
      },
      {
        key: 'reports-income-statement',
        label: 'Income Statement',
        url: '/accounting/reports/income-statement',
        parentKey: 'accounting-reports',
      },
      {
        key: 'reports-balance-sheet',
        label: 'Balance Sheet',
        url: '/accounting/reports/balance-sheet',
        parentKey: 'accounting-reports',
      },
      {
        key: 'reports-general-ledger',
        label: 'General Ledger',
        url: '/accounting/reports/general-ledger',
        parentKey: 'accounting-reports',
      },
    ],
  },
  {
    key: 'administration',
    label: 'Administration',
    isTitle: true,
  },
  {
    key: 'admin-users',
    label: 'Users',
    isTitle: false,
    icon: 'mgc_group_line',
    url: '/admin/users',
  },
  {
    key: 'admin-roles',
    label: 'Roles & Permissions',
    isTitle: false,
    icon: 'mgc_shield_line',
    url: '/admin/roles',
  },
  {
    key: 'admin-audit-logs',
    label: 'Audit Logs',
    isTitle: false,
    icon: 'mgc_time_line',
    url: '/admin/audit-logs',
  },
];

export { MENU_ITEMS };
