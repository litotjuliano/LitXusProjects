import { APICore } from "./apiCore";

const api = new APICore();

export interface Customer {
  id: string;
  code: string;
  companyName: string;
  contactPerson: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  creditLimit: number;
  paymentTermsDays: number;
  isActive: boolean;
}

export interface InvoiceLine {
  id?: string;
  description: string;
  quantity: number;
  unitOfMeasure: string | null;
  unitPrice: number;
  lineTotal?: number;
  taxCodeId: string | null;
  taxCodeName?: string | null;
}

export type InvoiceStatus = "Draft" | "Issued" | "PartiallyPaid" | "Paid" | "Void";

export interface Invoice {
  id: string;
  invoiceNumber: string | null;
  customerId: string;
  customerCode: string;
  customerName: string;
  invoiceDate: string;
  dueDate: string;
  status: InvoiceStatus;
  isOverdue: boolean;
  subTotal: number;
  sstAmount: number;
  totalAmount: number;
  amountPaid: number;
  outstandingBalance: number;
  notes: string | null;
  voidReason: string | null;
  lines: InvoiceLine[];
}

export type PaymentMethod = "BankTransfer" | "Cash" | "Cheque" | "OnlineGateway";
export type PaymentStatus = "Pending" | "Verified" | "Rejected";

export interface Payment {
  id: string;
  invoiceId: string;
  invoiceNumber: string | null;
  paymentDate: string;
  amount: number;
  method: PaymentMethod;
  referenceNumber: string | null;
  status: PaymentStatus;
  verifiedBy: string | null;
  verifiedAtUtc: string | null;
  bankAccountId: string | null;
  rejectReason: string | null;
}

export interface CreditNote {
  id: string;
  creditNoteNumber: string | null;
  invoiceId: string;
  invoiceNumber: string | null;
  reason: string;
  amount: number;
  status: string;
}

export interface SalesSettings {
  defaultReceivableAccountId: string | null;
  defaultRevenueAccountId: string | null;
  defaultTaxPayableAccountId: string | null;
  defaultCashAccountId: string | null;
  isConfigured: boolean;
}

// Customers
function listCustomers(includeInactive = false) {
  return api.get("/sales/customers", includeInactive ? { includeInactive: true } : null);
}

function createCustomer(payload: Pick<Customer, "code" | "companyName" | "contactPerson" | "email" | "phone" | "address" | "creditLimit" | "paymentTermsDays">) {
  return api.create("/sales/customers", payload);
}

function updateCustomer(id: string, payload: Pick<Customer, "companyName" | "contactPerson" | "email" | "phone" | "address" | "creditLimit" | "paymentTermsDays">) {
  return api.update(`/sales/customers/${id}`, payload);
}

function setCustomerActive(id: string, isActive: boolean) {
  return api.updatePatch(`/sales/customers/${id}/status`, { isActive });
}

// Invoices
function listInvoices(filters?: { status?: string; customerId?: string; dateFrom?: string; dateTo?: string }) {
  return api.get("/sales/invoices", filters ?? null);
}

function getInvoice(id: string) {
  return api.get(`/sales/invoices/${id}`, null);
}

function createInvoice(payload: { customerId: string; invoiceDate: string; dueDate: string; notes?: string; lines: InvoiceLine[] }) {
  return api.create("/sales/invoices", payload);
}

function updateInvoice(id: string, payload: { invoiceDate: string; dueDate: string; notes?: string; lines: InvoiceLine[] }) {
  return api.update(`/sales/invoices/${id}`, payload);
}

function issueInvoice(id: string) {
  return api.create(`/sales/invoices/${id}/issue`, {});
}

function voidInvoice(id: string, reason: string) {
  return api.create(`/sales/invoices/${id}/void`, { reason });
}

function recordPayment(invoiceId: string, payload: { paymentDate: string; amount: number; method: PaymentMethod; referenceNumber?: string; bankAccountId?: string }) {
  return api.create(`/sales/invoices/${invoiceId}/payments`, payload);
}

function getInvoicePdf(id: string) {
  return api.getFile(`/sales/invoices/${id}/pdf`, null);
}

// Payments
function listPayments(status?: string) {
  return api.get("/sales/payments", status ? { status } : null);
}

function verifyPayment(id: string) {
  return api.create(`/sales/payments/${id}/verify`, {});
}

function rejectPayment(id: string, reason: string) {
  return api.create(`/sales/payments/${id}/reject`, { reason });
}

// Credit Notes
function listCreditNotes() {
  return api.get("/sales/credit-notes", null);
}

function createCreditNote(payload: { invoiceId: string; reason: string; amount: number }) {
  return api.create("/sales/credit-notes", payload);
}

// Reports
function getSalesSummary(from: string, to: string, groupBy: "customer" | "product" | "month") {
  return api.get("/sales/reports/sales-summary", { from, to, groupBy });
}

function getArAging(asOfDate?: string) {
  return api.get("/sales/reports/aging", asOfDate ? { asOfDate } : null);
}

// Settings
function getSalesSettings() {
  return api.get("/sales/settings", null);
}

function configureSalesSettings(payload: { receivableAccountId: string; revenueAccountId: string; taxPayableAccountId: string; cashAccountId: string }) {
  return api.update("/sales/settings", payload);
}

export {
  listCustomers, createCustomer, updateCustomer, setCustomerActive,
  listInvoices, getInvoice, createInvoice, updateInvoice, issueInvoice, voidInvoice, recordPayment, getInvoicePdf,
  listPayments, verifyPayment, rejectPayment,
  listCreditNotes, createCreditNote,
  getSalesSummary, getArAging,
  getSalesSettings, configureSalesSettings,
};
