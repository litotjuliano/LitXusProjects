import { useEffect, useState } from "react";
import { PageBreadcrumb, ReportLetterhead } from "../../../components";
import { useCompanyProfile } from "../../../hooks";
import { getSalesSummary } from "../../../helpers/api/sales";
import { formatCurrency } from "../../../utils/currency";

interface SalesSummaryLine {
  groupKey: string;
  invoiceCount: number;
  subTotal: number;
  sstAmount: number;
  totalAmount: number;
}

interface SalesSummaryData {
  from: string;
  to: string;
  groupBy: string;
  lines: SalesSummaryLine[];
  grandTotal: number;
}

const today = () => new Date().toISOString().slice(0, 10);
const startOfYear = () => `${new Date().getFullYear()}-01-01`;

const SalesSummary = () => {
  const company = useCompanyProfile();
  const [from, setFrom] = useState(startOfYear());
  const [to, setTo] = useState(today());
  const [groupBy, setGroupBy] = useState<"customer" | "product" | "month">("customer");
  const [data, setData] = useState<SalesSummaryData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getSalesSummary(from, to, groupBy)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [from, to, groupBy]);

  return (
    <>
      <PageBreadcrumb title="Sales Summary" name="Sales Summary" breadCrumbItems={["Sales", "Reports", "Sales Summary"]}>
        <div className="flex items-center gap-2">
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="form-input text-sm" />
          <span className="text-sm text-slate-400">to</span>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="form-input text-sm" />
          <select value={groupBy} onChange={(e) => setGroupBy(e.target.value as typeof groupBy)} className="form-select text-sm">
            <option value="customer">By Customer</option>
            <option value="product">By Product</option>
            <option value="month">By Month</option>
          </select>
        </div>
      </PageBreadcrumb>

      <ReportLetterhead company={company} />

      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">{groupBy === "customer" ? "Customer" : groupBy === "product" ? "Product" : "Month"}</th>
                <th className="px-4 py-3 font-medium text-right">Invoices</th>
                <th className="px-4 py-3 font-medium text-right">Subtotal</th>
                <th className="px-4 py-3 font-medium text-right">SST</th>
                <th className="px-4 py-3 font-medium text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && data?.lines.length === 0 && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">No sales in this period.</td></tr>
              )}
              {!loading && data?.lines.map((l) => (
                <tr key={l.groupKey} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{l.groupKey}</td>
                  <td className="px-4 py-2 text-right">{l.invoiceCount}</td>
                  <td className="px-4 py-2 text-right">{formatCurrency(l.subTotal)}</td>
                  <td className="px-4 py-2 text-right">{formatCurrency(l.sstAmount)}</td>
                  <td className="px-4 py-2 text-right">{formatCurrency(l.totalAmount)}</td>
                </tr>
              ))}
            </tbody>
            {!loading && data && (
              <tfoot>
                <tr className="border-t-2 border-slate-300 dark:border-slate-600 font-medium">
                  <td className="px-4 py-3" colSpan={4}>Grand Total</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(data.grandTotal)}</td>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>
    </>
  );
};

export default SalesSummary;
