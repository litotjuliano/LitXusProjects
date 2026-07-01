import { APICore } from "./apiCore";

const api = new APICore();

export type AccountType = "Asset" | "Liability" | "Equity" | "Revenue" | "Expense";

export interface Account {
  id: string;
  code: string;
  name: string;
  type: AccountType;
  parentAccountId: string | null;
  isActive: boolean;
  balance: number;
}

export interface GLEntryLine {
  id?: string;
  accountId: string;
  debitAmount: number;
  creditAmount: number;
  lineDescription?: string;
}

export type GLEntryStatus = "Draft" | "Posted" | "Voided";

export interface GLEntry {
  id: string;
  entryNumber: string | null;
  entryDate: string;
  description: string;
  status: GLEntryStatus;
  lines: GLEntryLine[];
}

function listAccounts() {
  return api.get("/accounting/accounts", null);
}

function createAccount(payload: Pick<Account, "code" | "name" | "type" | "parentAccountId">) {
  return api.create("/accounting/accounts", payload);
}

function listGLEntries() {
  return api.get("/accounting/gl-entries", null);
}

function createGLEntry(payload: { entryDate: string; description: string; lines: GLEntryLine[] }) {
  return api.create("/accounting/gl-entries", payload);
}

function postGLEntry(id: string) {
  return api.create(`/accounting/gl-entries/${id}/post`, {});
}

function voidGLEntry(id: string, reason: string) {
  return api.create(`/accounting/gl-entries/${id}/void`, { reason });
}

export { listAccounts, createAccount, listGLEntries, createGLEntry, postGLEntry, voidGLEntry };
