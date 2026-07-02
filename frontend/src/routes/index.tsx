/* eslint-disable react-refresh/only-export-components */
import React from "react";
import { Navigate, Route, RouteProps } from "react-router-dom";

// components
import PrivateRoute from "./PrivateRoute";

// lazy load all the views

// auth
const Login = React.lazy(() => import("../pages/auth/Login"));
const Register = React.lazy(() => import("../pages/auth/Register"));
const RecoverPassword = React.lazy(() => import("../pages/auth/RecoverPassword"));
const LockScreen = React.lazy(() => import("../pages/auth/LockScreen"));

// dashboard
const Dashboard = React.lazy(() => import("../pages/accounting/Dashboard"));

// accounting (Phase 1)
const ChartOfAccounts = React.lazy(() => import("../pages/accounting/ChartOfAccounts"));
const GLEntries = React.lazy(() => import("../pages/accounting/GLEntries"));
const BankReconciliation = React.lazy(() => import("../pages/accounting/BankReconciliation"));
const TrialBalance = React.lazy(() => import("../pages/accounting/reports/TrialBalance"));
const IncomeStatement = React.lazy(() => import("../pages/accounting/reports/IncomeStatement"));
const BalanceSheet = React.lazy(() => import("../pages/accounting/reports/BalanceSheet"));
const GeneralLedger = React.lazy(() => import("../pages/accounting/reports/GeneralLedger"));

// admin
const AdminCompanyProfile = React.lazy(() => import("../pages/admin/CompanyProfile"));
const AdminUsers = React.lazy(() => import("../pages/admin/Users"));
const AdminRoles = React.lazy(() => import("../pages/admin/Roles"));
const AdminAuditLogs = React.lazy(() => import("../pages/admin/AuditLogs"));
const AdminLicense = React.lazy(() => import("../pages/admin/License"));

// error pages
const Maintenance = React.lazy(() => import('../pages/error/Maintenance'));
const ComingSoon = React.lazy(() => import('../pages/error/ComingSoon'));
const Error404 = React.lazy(() => import('../pages/error/Error404'));
const Error500 = React.lazy(() => import('../pages/error/Error500'));

export interface RoutesProps {
  path: RouteProps["path"];
  name?: string;
  element?: RouteProps["element"];
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  route?: any;
  exact?: boolean;
  icon?: string;
  header?: string;
  roles?: string[];
  children?: RoutesProps[];
}

// dashboards
const dashboardRoutes: RoutesProps = {
  path: "/home",
  name: "Dashboards",
  icon: "home",
  header: "Navigation",
  children: [
    {
      path: "/",
      name: "Root",
      element: <Navigate to='/dashboard' />,
      route: PrivateRoute,
    },
    {
      path: '/dashboard',
      name: "Dashboard",
      element: <Dashboard />,
      route: PrivateRoute,
    },
  ],
};

// Accounting (Phase 1)
const accountingRoutes: RoutesProps = {
  path: "/accounting",
  name: "Accounting",
  icon: "book",
  header: "Accounting",
  children: [
    {
      path: "/accounting/chart-of-accounts",
      name: "Chart of Accounts",
      element: <ChartOfAccounts />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/gl-entries",
      name: "GL Entries",
      element: <GLEntries />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/bank-reconciliation",
      name: "Bank Reconciliation",
      element: <BankReconciliation />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/reports/trial-balance",
      name: "Trial Balance",
      element: <TrialBalance />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/reports/income-statement",
      name: "Income Statement",
      element: <IncomeStatement />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/reports/balance-sheet",
      name: "Balance Sheet",
      element: <BalanceSheet />,
      route: PrivateRoute,
    },
    {
      path: "/accounting/reports/general-ledger",
      name: "General Ledger",
      element: <GeneralLedger />,
      route: PrivateRoute,
    },
  ],
};

// Administration
const adminRoutes: RoutesProps = {
  path: "/admin",
  name: "Administration",
  icon: "shield",
  header: "Administration",
  roles: ["Admin"],
  children: [
    {
      path: "/admin/company-profile",
      name: "Company Profile",
      element: <AdminCompanyProfile />,
      route: PrivateRoute,
    },
    {
      path: "/admin/users",
      name: "Users",
      element: <AdminUsers />,
      route: PrivateRoute,
    },
    {
      path: "/admin/roles",
      name: "Roles",
      element: <AdminRoles />,
      route: PrivateRoute,
    },
    {
      path: "/admin/audit-logs",
      name: "Audit Logs",
      element: <AdminAuditLogs />,
      route: PrivateRoute,
    },
    {
      path: "/admin/license",
      name: "License",
      element: <AdminLicense />,
      route: PrivateRoute,
    },
  ],
};

// auth
const authRoutes: RoutesProps[] = [
  {
    path: "/auth/login",
    name: "Login",
    element: <Login />,
    route: Route,
  },
  {
    path: "/auth/register",
    name: "Register",
    element: <Register />,
    route: Route,
  },
  {
    path: "/auth/recover-password",
    name: "Recover Password",
    element: <RecoverPassword />,
    route: Route,
  },
  {
    path: "/auth/lock-screen",
    name: "Lock Screen",
    element: <LockScreen />,
    route: Route,
  },
];

// public routes
const otherPublicRoutes = [
  {
    path: "*",
    name: "Error - 404",
    element: <Error404 />,
    route: Route,
  },
  {
    path: "/maintenance",
    name: "Maintenance",
    element: <Maintenance />,
    route: Route,
  },
  {
    path: "/coming-soon",
    name: "Coming Soon",
    element: <ComingSoon />,
    route: Route,
  },
  {
    path: "/error-404",
    name: "Error - 404",
    element: <Error404 />,
    route: Route,
  },
  {
    path: "/error-500",
    name: "Error - 500",
    element: <Error500 />,
    route: Route,
  },
];

// flatten the list of all nested routes
const flattenRoutes = (routes: RoutesProps[]) => {
  let flatRoutes: RoutesProps[] = [];

  routes = routes || [];
  routes.forEach((item: RoutesProps) => {
    flatRoutes.push(item);
    if (typeof item.children !== "undefined") {
      flatRoutes = [...flatRoutes, ...flattenRoutes(item.children)];
    }
  });
  return flatRoutes;
};

// All routes
const authProtectedRoutes = [
  dashboardRoutes,
  accountingRoutes,
  adminRoutes,
];
const publicRoutes = [...authRoutes, ...otherPublicRoutes];

const authProtectedFlattenRoutes = flattenRoutes([...authProtectedRoutes]);
const publicProtectedFlattenRoutes = flattenRoutes([...publicRoutes]);
export {
  publicRoutes,
  authProtectedRoutes,
  authProtectedFlattenRoutes,
  publicProtectedFlattenRoutes,
};
