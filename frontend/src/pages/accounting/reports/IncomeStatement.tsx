import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../../components";
import { getIncomeStatement, type IncomeStatement as IncomeStatementData } from "../../../helpers/api/reports";
import { formatCurrency } from "../../../utils/currency";

const startOfYear = () => `${new Date().getFullYear()}-01-01`;
const today = () => new Date().toISOString().slice(0, 10);

const IncomeStatement = () => {
  const [from, setFrom] = useState(startOfYear());
  const [to, setTo] = useState(today());
  const [data, setData] = useState<IncomeStatementData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getIncomeStatement(from, to)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [from, to]);

  return (
    <>
      <PageBreadcrumb title="Income Statement" name="Income Statement" breadCrumbItems={["Accounting", "Reports", "Income Statement"]}>
        <div className="flex items-center gap-2">
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="form-input text-sm" />
          <span className="text-slate-400 text-sm">to</span>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="form-input text-sm" />
        </div>
      </PageBreadcrumb>

      {loading && <div className="card"><div className="card-body text-sm text-slate-400">Loading…</div></div>}

      {!loading && data && (
        <div className="card">
          <div className="card-body">
            <h5 className="mb-2 font-medium text-slate-900 dark:text-slate-200">Revenue</h5>
            <table className="w-full text-sm mb-4">
              <tbody>
                {data.revenue.length === 0 && <tr><td className="py-1 text-slate-400">No revenue in this period.</td></tr>}
                {data.revenue.map((l) => (
                  <tr key={l.accountCode} className="border-b border-slate-100 dark:border-slate-800">
                    <td className="py-1.5">{l.accountCode} {l.accountName}</td>
                    <td className="py-1.5 text-right">{formatCurrency(l.amount)}</td>
                  </tr>
                ))}
                <tr className="font-medium border-t border-slate-300 dark:border-slate-600">
                  <td className="py-1.5">Total Revenue</td>
                  <td className="py-1.5 text-right">{formatCurrency(data.totalRevenue)}</td>
                </tr>
              </tbody>
            </table>

            <h5 className="mb-2 font-medium text-slate-900 dark:text-slate-200">Expenses</h5>
            <table className="w-full text-sm mb-4">
              <tbody>
                {data.expenses.length === 0 && <tr><td className="py-1 text-slate-400">No expenses in this period.</td></tr>}
                {data.expenses.map((l) => (
                  <tr key={l.accountCode} className="border-b border-slate-100 dark:border-slate-800">
                    <td className="py-1.5">{l.accountCode} {l.accountName}</td>
                    <td className="py-1.5 text-right">{formatCurrency(l.amount)}</td>
                  </tr>
                ))}
                <tr className="font-medium border-t border-slate-300 dark:border-slate-600">
                  <td className="py-1.5">Total Expenses</td>
                  <td className="py-1.5 text-right">{formatCurrency(data.totalExpenses)}</td>
                </tr>
              </tbody>
            </table>

            <div className={`flex justify-between text-base font-semibold pt-3 border-t-2 ${data.netIncome >= 0 ? "text-emerald-600" : "text-red-600"} border-slate-300 dark:border-slate-600`}>
              <span>Net {data.netIncome >= 0 ? "Income" : "Loss"}</span>
              <span>{formatCurrency(data.netIncome)}</span>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default IncomeStatement;
