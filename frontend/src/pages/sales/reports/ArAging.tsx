import { useEffect, useState } from "react";
import { PageBreadcrumb, ReportLetterhead } from "../../../components";
import { useCompanyProfile } from "../../../hooks";
import { getArAging } from "../../../helpers/api/sales";
import { formatCurrency } from "../../../utils/currency";

interface ArAgingLine {
  customerId: string;
  customerCode: string;
  customerName: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  over90Days: number;
  total: number;
}

interface ArAgingData {
  asOfDate: string;
  lines: ArAgingLine[];
  grandTotal: number;
}

const today = () => new Date().toISOString().slice(0, 10);

const ArAging = () => {
  const company = useCompanyProfile();
  const [asOfDate, setAsOfDate] = useState(today());
  const [data, setData] = useState<ArAgingData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getArAging(asOfDate)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [asOfDate]);

  const columnTotal = (key: keyof ArAgingLine) =>
    data?.lines.reduce((sum, l) => sum + (Number(l[key]) || 0), 0) ?? 0;

  return (
    <>
      <PageBreadcrumb title="AR Aging" name="AR Aging" breadCrumbItems={["Sales", "Reports", "AR Aging"]}>
        <input type="date" value={asOfDate} onChange={(e) => setAsOfDate(e.target.value)} className="form-input text-sm" />
      </PageBreadcrumb>

      <ReportLetterhead company={company} />

      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Customer</th>
                <th className="px-4 py-3 font-medium text-right">Current</th>
                <th className="px-4 py-3 font-medium text-right">1-30 Days</th>
                <th className="px-4 py-3 font-medium text-right">31-60 Days</th>
                <th className="px-4 py-3 font-medium text-right">61-90 Days</th>
                <th className="px-4 py-3 font-medium text-right">90+ Days</th>
                <th className="px-4 py-3 font-medium text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={7} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && data?.lines.length === 0 && (
                <tr><td colSpan={7} className="px-4 py-6 text-center text-slate-400">No outstanding balances as of this date.</td></tr>
              )}
              {!loading && data?.lines.map((l) => (
                <tr key={l.customerId} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{l.customerCode} — {l.customerName}</td>
                  <td className="px-4 py-2 text-right">{l.current ? formatCurrency(l.current) : ""}</td>
                  <td className="px-4 py-2 text-right">{l.days1To30 ? formatCurrency(l.days1To30) : ""}</td>
                  <td className="px-4 py-2 text-right">{l.days31To60 ? formatCurrency(l.days31To60) : ""}</td>
                  <td className="px-4 py-2 text-right">{l.days61To90 ? formatCurrency(l.days61To90) : ""}</td>
                  <td className="px-4 py-2 text-right">{l.over90Days ? formatCurrency(l.over90Days) : ""}</td>
                  <td className="px-4 py-2 text-right font-medium">{formatCurrency(l.total)}</td>
                </tr>
              ))}
            </tbody>
            {!loading && data && (
              <tfoot>
                <tr className="border-t-2 border-slate-300 dark:border-slate-600 font-medium">
                  <td className="px-4 py-3">Total</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(columnTotal("current"))}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(columnTotal("days1To30"))}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(columnTotal("days31To60"))}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(columnTotal("days61To90"))}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(columnTotal("over90Days"))}</td>
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

export default ArAging;
