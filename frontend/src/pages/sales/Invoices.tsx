import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";

import { PageBreadcrumb, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listInvoices,
  getInvoice,
  createInvoice,
  updateInvoice,
  issueInvoice,
  voidInvoice,
  recordPayment,
  getInvoicePdf,
  listCustomers,
  type Customer,
  type Invoice,
  type InvoiceStatus,
  type PaymentMethod,
} from "../../helpers/api/sales";
import { listTaxCodes, type TaxCode } from "../../helpers/api/taxCodes";
import { listBankAccounts, type BankAccount } from "../../helpers/api/bankReconciliation";
import { formatCurrency } from "../../utils/currency";
import { downloadBlob } from "../../utils/fileDownload";

interface InvoiceLineFormValues {
  description: string;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  taxCodeId: string;
}

interface InvoiceFormValues {
  customerId: string;
  invoiceDate: string;
  dueDate: string;
  notes: string;
  lines: InvoiceLineFormValues[];
}

interface PaymentFormValues {
  paymentDate: string;
  amount: number;
  method: PaymentMethod;
  referenceNumber: string;
  bankAccountId: string;
}

const statusStyles: Record<InvoiceStatus, string> = {
  Draft: "text-slate-500",
  Issued: "text-blue-600",
  PartiallyPaid: "text-amber-600",
  Paid: "text-emerald-600",
  Void: "text-red-500 line-through",
};

const emptyLine: InvoiceLineFormValues = { description: "", quantity: 1, unitOfMeasure: "", unitPrice: 0, taxCodeId: "" };

const todayIso = () => new Date().toISOString().slice(0, 10);
const plusDays = (days: number) => {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
};

const Invoices = () => {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editingInvoice, setEditingInvoice] = useState<Invoice | null>(null);
  const [viewingInvoice, setViewingInvoice] = useState<Invoice | null>(null);
  const [paymentError, setPaymentError] = useState<string | null>(null);

  const fetchInvoices = () => {
    setLoading(true);
    listInvoices(statusFilter ? { status: statusFilter } : undefined)
      .then((res) => setInvoices(res.data?.data ?? []))
      .catch(() => setInvoices([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchInvoices, [statusFilter]);

  useEffect(() => {
    listCustomers().then((res) => setCustomers(res.data?.data ?? [])).catch(() => setCustomers([]));
    listTaxCodes().then((res) => setTaxCodes(res.data?.data ?? [])).catch(() => setTaxCodes([]));
    listBankAccounts().then((res) => setBankAccounts(res.data?.data ?? [])).catch(() => setBankAccounts([]));
  }, []);

  const { register, control, handleSubmit, watch, reset } = useForm<InvoiceFormValues>({
    defaultValues: {
      customerId: "",
      invoiceDate: todayIso(),
      dueDate: plusDays(30),
      notes: "",
      lines: [emptyLine],
    },
  });
  const { fields, append, remove } = useFieldArray({ control, name: "lines" });
  const watchedLines = watch("lines");

  const lineTotal = (line: InvoiceLineFormValues) => (Number(line.quantity) || 0) * (Number(line.unitPrice) || 0);
  const lineTax = (line: InvoiceLineFormValues) => {
    const tc = taxCodes.find((t) => t.id === line.taxCodeId);
    return tc ? lineTotal(line) * (tc.rate / 100) : 0;
  };
  const subTotal = watchedLines.reduce((sum, l) => sum + lineTotal(l), 0);
  const sstAmount = watchedLines.reduce((sum, l) => sum + lineTax(l), 0);

  const {
    register: registerPayment,
    handleSubmit: handlePaymentSubmit,
    watch: watchPayment,
    reset: resetPayment,
  } = useForm<PaymentFormValues>({
    defaultValues: { paymentDate: todayIso(), amount: 0, method: "BankTransfer", referenceNumber: "", bankAccountId: "" },
  });
  const paymentMethod = watchPayment("method");

  const closeModal = () => {
    setShowModal(false);
    setFormError(null);
    setEditingInvoice(null);
    reset();
  };

  const openNewModal = () => {
    setFormError(null);
    setEditingInvoice(null);
    reset({ customerId: "", invoiceDate: todayIso(), dueDate: plusDays(30), notes: "", lines: [emptyLine] });
    setShowModal(true);
  };

  const openEditModal = (invoice: Invoice) => {
    setFormError(null);
    setEditingInvoice(invoice);
    reset({
      customerId: invoice.customerId,
      invoiceDate: invoice.invoiceDate,
      dueDate: invoice.dueDate,
      notes: invoice.notes ?? "",
      lines: invoice.lines.map((l) => ({
        description: l.description,
        quantity: l.quantity,
        unitOfMeasure: l.unitOfMeasure ?? "",
        unitPrice: l.unitPrice,
        taxCodeId: l.taxCodeId ?? "",
      })),
    });
    setShowModal(true);
  };

  const openViewModal = (invoice: Invoice) => {
    setPaymentError(null);
    resetPayment({ paymentDate: todayIso(), amount: invoice.outstandingBalance, method: "BankTransfer", referenceNumber: "", bankAccountId: "" });
    getInvoice(invoice.id)
      .then((res) => setViewingInvoice(res.data?.data ?? invoice))
      .catch(() => setViewingInvoice(invoice));
  };

  const submit = async (values: InvoiceFormValues) => {
    setFormError(null);
    const payload = {
      customerId: values.customerId,
      invoiceDate: values.invoiceDate,
      dueDate: values.dueDate,
      notes: values.notes || undefined,
      lines: values.lines.map((l) => ({
        description: l.description,
        quantity: Number(l.quantity),
        unitOfMeasure: l.unitOfMeasure || null,
        unitPrice: Number(l.unitPrice),
        taxCodeId: l.taxCodeId || null,
      })),
    };
    try {
      if (editingInvoice) {
        await updateInvoice(editingInvoice.id, payload);
      } else {
        const res = await createInvoice(payload);
        const warning = res.data?.meta?.creditLimitWarning;
        if (warning) window.alert(warning);
      }
      closeModal();
      fetchInvoices();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not save the invoice. Please try again.");
    }
  };

  const handleIssue = async (invoice: Invoice) => {
    setBusyId(invoice.id);
    try {
      await issueInvoice(invoice.id);
      await fetchInvoices();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not issue this invoice. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

  const handleVoid = async (invoice: Invoice) => {
    const reason = window.prompt("Reason for voiding this invoice:");
    if (!reason) return;
    setBusyId(invoice.id);
    try {
      await voidInvoice(invoice.id, reason);
      await fetchInvoices();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not void this invoice. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

  const handleDownloadPdf = async (invoice: Invoice) => {
    const res = await getInvoicePdf(invoice.id);
    downloadBlob(res.data, `invoice-${invoice.invoiceNumber ?? invoice.id}.pdf`);
  };

  const submitPayment = async (values: PaymentFormValues) => {
    if (!viewingInvoice) return;
    setPaymentError(null);
    try {
      await recordPayment(viewingInvoice.id, {
        paymentDate: values.paymentDate,
        amount: Number(values.amount),
        method: values.method,
        referenceNumber: values.referenceNumber || undefined,
        bankAccountId: values.bankAccountId || undefined,
      });
      const refreshed = await getInvoice(viewingInvoice.id).then((res) => res.data?.data ?? viewingInvoice);
      setViewingInvoice(refreshed);
      resetPayment({ paymentDate: todayIso(), amount: refreshed.outstandingBalance, method: "BankTransfer", referenceNumber: "", bankAccountId: "" });
      fetchInvoices();
    } catch (err) {
      setPaymentError(typeof err === "string" ? err : "Could not record this payment. Please try again.");
    }
  };

  const columns: DataTableColumn<Invoice>[] = [
    {
      key: "number", header: "Number",
      render: (i) => (
        <button onClick={() => openViewModal(i)} className="font-medium text-primary hover:underline">
          {i.invoiceNumber ?? "(Draft)"}
        </button>
      ),
      sortAccessor: (i) => i.invoiceNumber ?? "",
    },
    { key: "customer", header: "Customer", render: (i) => i.customerName, sortAccessor: (i) => i.customerName },
    { key: "date", header: "Date", render: (i) => i.invoiceDate, sortAccessor: (i) => i.invoiceDate },
    { key: "dueDate", header: "Due", render: (i) => i.dueDate, sortAccessor: (i) => i.dueDate },
    {
      key: "status", header: "Status",
      render: (i) => (
        <span className={statusStyles[i.status]}>
          {i.status}
          {i.isOverdue && i.status !== "Void" && <span className="ml-1 text-red-600">(Overdue)</span>}
        </span>
      ),
      sortAccessor: (i) => i.status,
    },
    {
      key: "total", header: "Total", className: "text-right", headerClassName: "text-right",
      render: (i) => formatCurrency(i.totalAmount), sortAccessor: (i) => i.totalAmount,
    },
    {
      key: "outstanding", header: "Outstanding", className: "text-right", headerClassName: "text-right",
      render: (i) => formatCurrency(i.outstandingBalance), sortAccessor: (i) => i.outstandingBalance,
    },
    {
      key: "actions", header: "", className: "text-right",
      render: (i) => (
        <div className="flex justify-end gap-3">
          {i.status === "Draft" && (
            <>
              <button onClick={() => openEditModal(i)} className="text-xs font-medium text-primary hover:underline">Edit</button>
              <button onClick={() => handleIssue(i)} disabled={busyId === i.id} className="text-xs font-medium text-primary hover:underline disabled:opacity-50">Issue</button>
            </>
          )}
          {(i.status === "Issued" || i.status === "PartiallyPaid") && (
            <button onClick={() => handleVoid(i)} disabled={busyId === i.id} className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50">Void</button>
          )}
        </div>
      ),
    },
  ];

  return (
    <>
      <PageBreadcrumb title="Invoices" name="Invoices" breadCrumbItems={["Sales", "Invoices"]}>
        <button className="btn text-white bg-primary text-sm" onClick={openNewModal}>+ New Invoice</button>
      </PageBreadcrumb>

      <div className="mb-3">
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="form-select text-sm w-56">
          <option value="">All statuses</option>
          <option value="Draft">Draft</option>
          <option value="Issued">Issued</option>
          <option value="PartiallyPaid">Partially Paid</option>
          <option value="Paid">Paid</option>
          <option value="Void">Void</option>
        </select>
      </div>

      <DataTable<Invoice>
        columns={columns}
        data={invoices}
        rowKey={(i) => i.id}
        loading={loading}
        emptyMessage="No invoices yet."
        searchPlaceholder="Search by number or customer…"
        searchAccessor={(i) => `${i.invoiceNumber ?? ""} ${i.customerName}`}
      />

      <ModalLayout showModal={showModal} toggleModal={closeModal} panelClassName="w-full max-w-4xl bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">
          {editingInvoice ? "Edit Invoice" : "New Invoice"}
        </h5>
        <form className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Customer</label>
              <select {...register("customerId", { required: true })} className="form-select w-full">
                <option value="">Select customer…</option>
                {customers.filter((c) => c.isActive).map((c) => (
                  <option key={c.id} value={c.id}>{c.code} — {c.companyName}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Invoice Date</label>
              <input type="date" {...register("invoiceDate", { required: true })} className="form-input w-full" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Due Date</label>
              <input type="date" {...register("dueDate", { required: true })} className="form-input w-full" />
            </div>
          </div>

          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400">
                <th className="pb-2 font-medium">Description</th>
                <th className="w-20 pb-2 font-medium">Qty</th>
                <th className="w-24 pb-2 font-medium">UOM</th>
                <th className="w-28 pb-2 text-right font-medium">Unit Price</th>
                <th className="w-36 pb-2 font-medium">Tax</th>
                <th className="w-28 pb-2 text-right font-medium">Line Total</th>
                <th className="w-8" />
              </tr>
            </thead>
            <tbody>
              {fields.map((field, index) => (
                <tr key={field.id}>
                  <td className="pr-2 py-1">
                    <input {...register(`lines.${index}.description`, { required: true })} className="form-input w-full" />
                  </td>
                  <td className="pr-2 py-1">
                    <input type="number" step="0.01" {...register(`lines.${index}.quantity`, { valueAsNumber: true })} className="form-input w-full" />
                  </td>
                  <td className="pr-2 py-1">
                    <input {...register(`lines.${index}.unitOfMeasure`)} placeholder="pcs" className="form-input w-full" />
                  </td>
                  <td className="pr-2 py-1">
                    <input type="number" step="0.01" {...register(`lines.${index}.unitPrice`, { valueAsNumber: true })} className="form-input w-full text-right" />
                  </td>
                  <td className="pr-2 py-1">
                    <select {...register(`lines.${index}.taxCodeId`)} className="form-select w-full">
                      <option value="">No tax</option>
                      {taxCodes.map((t) => (
                        <option key={t.id} value={t.id}>{t.code} ({t.rate}%)</option>
                      ))}
                    </select>
                  </td>
                  <td className="py-1 text-right font-mono">{formatCurrency(lineTotal(watchedLines[index] ?? emptyLine))}</td>
                  <td className="py-1 text-center">
                    <button type="button" onClick={() => remove(index)} className="text-slate-400 hover:text-red-600" aria-label="Remove line">×</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <button type="button" onClick={() => append(emptyLine)} className="text-sm font-medium text-primary hover:underline">
            + Add Line
          </button>

          <div>
            <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Notes</label>
            <input type="text" {...register("notes")} className="form-input w-full" />
          </div>

          <div className="flex flex-col items-end gap-1 border-t border-slate-200 dark:border-slate-700 pt-3 text-sm">
            <span>Subtotal: <span className="font-mono">{formatCurrency(subTotal)}</span></span>
            <span>SST: <span className="font-mono">{formatCurrency(sstAmount)}</span></span>
            <span className="font-medium">Total: <span className="font-mono">{formatCurrency(subTotal + sstAmount)}</span></span>
          </div>

          {formError && <div className="text-sm text-red-600">{formError}</div>}

          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={closeModal}>Cancel</button>
            <button type="button" className="btn text-white bg-primary text-sm" onClick={handleSubmit(submit)}>
              {editingInvoice ? "Save Changes" : "Save as Draft"}
            </button>
          </div>
        </form>
      </ModalLayout>

      <ModalLayout showModal={viewingInvoice !== null} toggleModal={() => setViewingInvoice(null)} panelClassName="w-full max-w-3xl bg-white dark:bg-slate-800 p-6">
        {viewingInvoice && (
          <div className="space-y-4">
            <div className="flex items-start justify-between">
              <div>
                <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200">
                  {viewingInvoice.invoiceNumber ?? "(Draft)"} — {viewingInvoice.customerName}
                </h5>
                <p className={`text-sm ${statusStyles[viewingInvoice.status]}`}>
                  {viewingInvoice.status}{viewingInvoice.isOverdue && viewingInvoice.status !== "Void" ? " (Overdue)" : ""}
                </p>
              </div>
              <div className="text-right text-sm">
                <div>Total: <span className="font-mono">{formatCurrency(viewingInvoice.totalAmount)}</span></div>
                <div>Outstanding: <span className="font-mono">{formatCurrency(viewingInvoice.outstandingBalance)}</span></div>
                <button type="button" onClick={() => handleDownloadPdf(viewingInvoice)} className="mt-1 text-xs font-medium text-primary hover:underline">
                  Download PDF
                </button>
              </div>
            </div>

            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-slate-500 dark:text-slate-400">
                  <th className="pb-2 font-medium">Description</th>
                  <th className="pb-2 font-medium">Qty</th>
                  <th className="pb-2 font-medium">UOM</th>
                  <th className="pb-2 text-right font-medium">Unit Price</th>
                  <th className="pb-2 text-right font-medium">Line Total</th>
                </tr>
              </thead>
              <tbody>
                {viewingInvoice.lines.map((l, idx) => (
                  <tr key={l.id ?? idx}>
                    <td className="py-1">{l.description}</td>
                    <td className="py-1">{l.quantity}</td>
                    <td className="py-1">{l.unitOfMeasure ?? "—"}</td>
                    <td className="py-1 text-right font-mono">{formatCurrency(l.unitPrice)}</td>
                    <td className="py-1 text-right font-mono">{formatCurrency(l.lineTotal ?? l.quantity * l.unitPrice)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            {viewingInvoice.status !== "Draft" && viewingInvoice.status !== "Void" && viewingInvoice.outstandingBalance > 0 && (
              <div className="border-t border-slate-200 dark:border-slate-700 pt-4">
                <h6 className="text-sm font-medium text-slate-700 dark:text-slate-300 mb-3">Record Payment</h6>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Payment Date</label>
                    <input type="date" {...registerPayment("paymentDate", { required: true })} className="form-input w-full" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Amount (RM)</label>
                    <input type="number" step="0.01" {...registerPayment("amount", { valueAsNumber: true })} className="form-input w-full" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Method</label>
                    <select {...registerPayment("method")} className="form-select w-full">
                      <option value="BankTransfer">Bank Transfer</option>
                      <option value="Cash">Cash</option>
                      <option value="Cheque">Cheque</option>
                      <option value="OnlineGateway">Online Gateway</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Reference No.</label>
                    <input type="text" {...registerPayment("referenceNumber")} className="form-input w-full" />
                  </div>
                  {paymentMethod !== "Cash" && (
                    <div className="col-span-2">
                      <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Bank Account (optional)</label>
                      <select {...registerPayment("bankAccountId")} className="form-select w-full">
                        <option value="">Use default cash account</option>
                        {bankAccounts.map((b) => (
                          <option key={b.id} value={b.id}>{b.bankName} — {b.accountNumber}</option>
                        ))}
                      </select>
                    </div>
                  )}
                </div>
                {paymentError && <div className="text-sm text-red-600 mt-2">{paymentError}</div>}
                <div className="flex justify-end pt-3">
                  <button type="button" className="btn text-white bg-primary text-sm" onClick={handlePaymentSubmit(submitPayment)}>
                    Record Payment
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </ModalLayout>
    </>
  );
};

export default Invoices;
