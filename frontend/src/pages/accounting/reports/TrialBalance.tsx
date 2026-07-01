import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../../components";
import { getTrialBalance, type TrialBalance as TrialBalanceData } from "../../../helpers/api/reports";
import { formatCurrency } from "../../../utils/currency";

const today = () => new Date().toISOString().slice(0, 10);

const TrialBalance = () => {
  const [asOfDate, setAsOfDate] = useState(today());
  const [data, setData] = useState<TrialBalanceData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getTrialBalance(asOfDate)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [asOfDate]);

  const isBalanced = data && data.totalDebit === data.totalCredit;

  return (
    <>
      <PageBreadcrumb title="Trial Balance" name="Trial Balance" breadCrumbItems={["Accounting", "Reports", "Trial Balance"]}>
        <input
          type="date"
          value={asOfDate}
          onChange={(e) => setAsOfDate(e.target.value)}
          className="form-input text-sm"
        />
      </PageBreadcrumb>

      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Code</th>
                <th className="px-4 py-3 font-medium">Account</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium text-right">Debit</th>
                <th className="px-4 py-3 font-medium text-right">Credit</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && data?.lines.length === 0 && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">No posted activity as of this date.</td></tr>
              )}
              {!loading && data?.lines.map((l) => (
                <tr key={l.accountCode} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{l.accountCode}</td>
                  <td className="px-4 py-2">{l.accountName}</td>
                  <td className="px-4 py-2 text-slate-500 dark:text-slate-400">{l.accountType}</td>
                  <td className="px-4 py-2 text-right">{l.debit ? formatCurrency(l.debit) : ""}</td>
                  <td className="px-4 py-2 text-right">{l.credit ? formatCurrency(l.credit) : ""}</td>
                </tr>
              ))}
            </tbody>
            {!loading && data && (
              <tfoot>
                <tr className="border-t-2 border-slate-300 dark:border-slate-600 font-medium">
                  <td className="px-4 py-3" colSpan={3}>Total</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(data.totalDebit)}</td>
                  <td className="px-4 py-3 text-right">{formatCurrency(data.totalCredit)}</td>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>

      {!loading && data && (
        <p className={`mt-3 text-sm font-medium ${isBalanced ? "text-emerald-600" : "text-red-600"}`}>
          {isBalanced ? "✓ Balanced" : "⚠ Out of balance — this should never happen with posted entries only"}
        </p>
      )}
    </>
  );
};

export default TrialBalance;
