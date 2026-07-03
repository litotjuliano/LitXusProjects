import { APICore } from "./apiCore";

const api = new APICore();

export interface TrialBalanceLine {
  accountCode: string;
  accountName: string;
  accountType: string;
  debit: number;
  credit: number;
}

export interface TrialBalance {
  asOfDate: string;
  lines: TrialBalanceLine[];
  totalDebit: number;
  totalCredit: number;
}

export interface BalanceSheetLine {
  accountCode: string;
  accountName: string;
  balance: number;
}

export interface BalanceSheet {
  asOfDate: string;
  assets: BalanceSheetLine[];
  liabilities: BalanceSheetLine[];
  equity: BalanceSheetLine[];
  currentYearEarnings: number;
  totalAssets: number;
  totalLiabilitiesAndEquity: number;
}

export interface IncomeStatementLine {
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface IncomeStatement {
  from: string;
  to: string;
  revenue: IncomeStatementLine[];
  expenses: IncomeStatementLine[];
  totalRevenue: number;
  totalExpenses: number;
  netIncome: number;
}

export interface GeneralLedgerLine {
  glEntryId: string;
  entryDate: string;
  entryNumber: string | null;
  description: string;
  debit: number;
  credit: number;
  runningBalance: number;
}

export interface GeneralLedger {
  accountCode: string;
  accountName: string;
  from: string;
  to: string;
  lines: GeneralLedgerLine[];
  endingBalance: number;
}

function getTrialBalance(asOfDate?: string) {
  return api.get("/accounting/reports/trial-balance", asOfDate ? { asOfDate } : null);
}

function getBalanceSheet(asOfDate?: string) {
  return api.get("/accounting/reports/balance-sheet", asOfDate ? { asOfDate } : null);
}

function getIncomeStatement(from?: string, to?: string) {
  return api.get("/accounting/reports/income-statement", from && to ? { from, to } : null);
}

function getGeneralLedger(accountId: string, from?: string, to?: string) {
  const params: Record<string, string> = { accountId };
  if (from) params.from = from;
  if (to) params.to = to;
  return api.get("/accounting/reports/general-ledger", params);
}

type ExportFormat = "pdf" | "excel";

function exportTrialBalance(format: ExportFormat, asOfDate?: string) {
  return api.getFile(`/accounting/reports/trial-balance/${format}`, asOfDate ? { asOfDate } : null);
}

function exportBalanceSheet(format: ExportFormat, asOfDate?: string) {
  return api.getFile(`/accounting/reports/balance-sheet/${format}`, asOfDate ? { asOfDate } : null);
}

function exportIncomeStatement(format: ExportFormat, from?: string, to?: string) {
  return api.getFile(`/accounting/reports/income-statement/${format}`, from && to ? { from, to } : null);
}

function exportGeneralLedger(format: ExportFormat, accountId: string, from?: string, to?: string) {
  const params: Record<string, string> = { accountId };
  if (from) params.from = from;
  if (to) params.to = to;
  return api.getFile(`/accounting/reports/general-ledger/${format}`, params);
}

export {
  getTrialBalance,
  getBalanceSheet,
  getIncomeStatement,
  getGeneralLedger,
  exportTrialBalance,
  exportBalanceSheet,
  exportIncomeStatement,
  exportGeneralLedger,
};
