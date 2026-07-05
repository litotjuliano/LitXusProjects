import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import { listCreditNotes, createCreditNote, listInvoices, type CreditNote, type Invoice } from "../../helpers/api/sales";
import { formatCurrency } from "../../utils/currency";

interface CreditNoteFormValues {
  invoiceId: string;
  reason: string;
  amount: number;
}

const CreditNotes = () => {
  const [creditNotes, setCreditNotes] = useState<CreditNote[]>([]);
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const fetchCreditNotes = () => {
    setLoading(true);
    listCreditNotes()
      .then((res) => setCreditNotes(res.data?.data ?? []))
      .catch(() => setCreditNotes([]))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchCreditNotes();
    listInvoices().then((res) => setInvoices(res.data?.data ?? [])).catch(() => setInvoices([]));
  }, []);

  const eligibleInvoices = invoices.filter((i) => i.outstandingBalance > 0 && i.status !== "Draft" && i.status !== "Void");

  const schemaResolver = yupResolver<CreditNoteFormValues>(
    yup.object().shape({
      invoiceId: yup.string().required("Please select an invoice"),
      reason: yup.string().required("Please enter a reason"),
      amount: yup.number().moreThan(0, "Amount must be greater than 0").required("Please enter an amount"),
    })
  );

  const onSubmit = async (formData: CreditNoteFormValues) => {
    setFormError(null);
    try {
      await createCreditNote(formData);
      setShowModal(false);
      fetchCreditNotes();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not create the credit note. Please try again.");
    }
  };

  const columns: DataTableColumn<CreditNote>[] = [
    { key: "number", header: "Number", render: (c) => c.creditNoteNumber ?? "—", sortAccessor: (c) => c.creditNoteNumber ?? "" },
    { key: "invoiceNumber", header: "Invoice", render: (c) => c.invoiceNumber ?? "—", sortAccessor: (c) => c.invoiceNumber ?? "" },
    { key: "reason", header: "Reason", render: (c) => c.reason },
    {
      key: "amount", header: "Amount", className: "text-right", headerClassName: "text-right",
      render: (c) => formatCurrency(c.amount), sortAccessor: (c) => c.amount,
    },
    { key: "status", header: "Status", render: (c) => c.status },
  ];

  return (
    <>
      <PageBreadcrumb title="Credit Notes" name="Credit Notes" breadCrumbItems={["Sales", "Credit Notes"]}>
        <button className="btn text-white bg-primary text-sm" onClick={() => { setFormError(null); setShowModal(true); }}>
          + New Credit Note
        </button>
      </PageBreadcrumb>

      <DataTable<CreditNote>
        columns={columns}
        data={creditNotes}
        rowKey={(c) => c.id}
        loading={loading}
        emptyMessage="No credit notes yet."
        searchPlaceholder="Search by number, invoice, or reason…"
        searchAccessor={(c) => `${c.creditNoteNumber ?? ""} ${c.invoiceNumber ?? ""} ${c.reason}`}
      />

      <ModalLayout showModal={showModal} toggleModal={() => setShowModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Credit Note</h5>
        <VerticalForm<CreditNoteFormValues> onSubmit={onSubmit} resolver={schemaResolver} defaultValues={{ amount: 0 }}>
          <FormInput label="Invoice" type="select" name="invoiceId" containerClass="mb-4" className="form-select w-full" labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">
            <option value="" disabled>Select invoice…</option>
            {eligibleInvoices.map((i) => (
              <option key={i.id} value={i.id}>
                {i.invoiceNumber} — {i.customerName} (outstanding {formatCurrency(i.outstandingBalance)})
              </option>
            ))}
          </FormInput>
          <FormInput label="Reason" type="text" name="reason" containerClass="mb-4" className="form-input w-full" labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2" />
          <FormInput label="Amount (RM)" type="number" name="amount" containerClass="mb-4" className="form-input w-full" labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2" />
          <div className="text-sm text-red-600 mb-3">{formError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowModal(false)}>Cancel</button>
            <button type="submit" className="btn text-white bg-primary text-sm">Create Credit Note</button>
          </div>
        </VerticalForm>
      </ModalLayout>
    </>
  );
};

export default CreditNotes;
