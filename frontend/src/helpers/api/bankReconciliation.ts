import { APICore } from "./apiCore";

const api = new APICore();

export interface BankAccount {
  id: string;
  accountId: string;
  accountCode: string;
  accountName: string;
  bankName: string;
  accountNumber: string;
  currency: string;
}

export interface BankStatementLine {
  id: string;
  bankAccountId: string;
  transactionDate: string;
  description: string;
  amount: number;
  isReconciled: boolean;
  matchedGLEntryLineId: string | null;
}

export interface UnmatchedGLEntryLine {
  glEntryLineId: string;
  glEntryId: string;
  entryDate: string;
  entryNumber: string | null;
  description: string;
  debitAmount: number;
  creditAmount: number;
}

export interface ReconciliationStatus {
  totalStatementLines: number;
  matchedStatementLines: number;
  unmatchedStatementLines: number;
}

function listBankAccounts() {
  return api.get("/accounting/bank-accounts", null);
}

function createBankAccount(payload: { accountId: string; bankName: string; accountNumber: string }) {
  return api.create("/accounting/bank-accounts", payload);
}

function listStatementLines(bankAccountId: string) {
  return api.get(`/accounting/bank-accounts/${bankAccountId}/statement-lines`, null);
}

function importStatementLines(bankAccountId: string, file: File) {
  return api.createWithFile(`/accounting/bank-accounts/${bankAccountId}/statement-lines/import`, { file });
}

function listUnmatchedGLLines(bankAccountId: string) {
  return api.get(`/accounting/bank-accounts/${bankAccountId}/unmatched-gl-lines`, null);
}

function getReconciliationStatus(bankAccountId: string) {
  return api.get(`/accounting/bank-accounts/${bankAccountId}/reconciliation-status`, null);
}

function matchStatementLine(statementLineId: string, glEntryLineId: string) {
  return api.create(`/accounting/bank-statement-lines/${statementLineId}/match`, { glEntryLineId });
}

function unmatchStatementLine(statementLineId: string) {
  return api.create(`/accounting/bank-statement-lines/${statementLineId}/unmatch`, {});
}

export {
  listBankAccounts,
  createBankAccount,
  listStatementLines,
  importStatementLines,
  listUnmatchedGLLines,
  getReconciliationStatus,
  matchStatementLine,
  unmatchStatementLine,
};
