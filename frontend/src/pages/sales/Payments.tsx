import { useEffect, useState } from "react";

import { PageBreadcrumb, DataTable, type DataTableColumn } from "../../components";
import { listPayments, verifyPayment, rejectPayment, type Payment, type PaymentStatus } from "../../helpers/api/sales";
import { formatCurrency } from "../../utils/currency";

const statusStyles: Record<PaymentStatus, string> = {
  Pending: "text-amber-600",
  Verified: "text-emerald-600",
  Rejected: "text-red-500 line-through",
};

const Payments = () => {
  const [payments, setPayments] = useState<Payment[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const fetchPayments = () => {
    setLoading(true);
    listPayments(statusFilter || undefined)
      .then((res) => setPayments(res.data?.data ?? []))
      .catch(() => setPayments([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchPayments, [statusFilter]);

  const handleVerify = async (payment: Payment) => {
    setBusyId(payment.id);
    try {
      await verifyPayment(payment.id);
      await fetchPayments();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not verify this payment. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

  const handleReject = async (payment: Payment) => {
    const reason = window.prompt("Reason for rejecting this payment:");
    if (!reason) return;
    setBusyId(payment.id);
    try {
      await rejectPayment(payment.id, reason);
      await fetchPayments();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not reject this payment. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

  const columns: DataTableColumn<Payment>[] = [
    { key: "invoiceNumber", header: "Invoice", render: (p) => p.invoiceNumber ?? "—", sortAccessor: (p) => p.invoiceNumber ?? "" },
    { key: "date", header: "Payment Date", render: (p) => p.paymentDate, sortAccessor: (p) => p.paymentDate },
    {
      key: "amount", header: "Amount", className: "text-right", headerClassName: "text-right",
      render: (p) => formatCurrency(p.amount), sortAccessor: (p) => p.amount,
    },
    { key: "method", header: "Method", render: (p) => p.method },
    { key: "referenceNumber", header: "Reference", render: (p) => p.referenceNumber ?? "—" },
    {
      key: "status", header: "Status",
      render: (p) => (
        <span className={statusStyles[p.status]}>
          {p.status}
          {p.status === "Rejected" && p.rejectReason && <span className="ml-1 text-xs text-slate-400">({p.rejectReason})</span>}
        </span>
      ),
      sortAccessor: (p) => p.status,
    },
    {
      key: "actions", header: "", className: "text-right",
      render: (p) => (
        p.status === "Pending" ? (
          <div className="flex justify-end gap-3">
            <button onClick={() => handleVerify(p)} disabled={busyId === p.id} className="text-xs font-medium text-primary hover:underline disabled:opacity-50">
              Verify
            </button>
            <button onClick={() => handleReject(p)} disabled={busyId === p.id} className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50">
              Reject
            </button>
          </div>
        ) : null
      ),
    },
  ];

  return (
    <>
      <PageBreadcrumb title="Payments" name="Payments" breadCrumbItems={["Sales", "Payments"]} />

      <div className="mb-3">
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="form-select text-sm w-56">
          <option value="">All statuses</option>
          <option value="Pending">Pending</option>
          <option value="Verified">Verified</option>
          <option value="Rejected">Rejected</option>
        </select>
      </div>

      <DataTable<Payment>
        columns={columns}
        data={payments}
        rowKey={(p) => p.id}
        loading={loading}
        emptyMessage="No payments recorded yet."
        searchPlaceholder="Search by invoice or reference…"
        searchAccessor={(p) => `${p.invoiceNumber ?? ""} ${p.referenceNumber ?? ""}`}
      />
    </>
  );
};

export default Payments;
