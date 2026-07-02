import { useEffect, useState } from "react";
import { PageBreadcrumb, ReportLetterhead } from "../../../components";
import { useCompanyProfile } from "../../../hooks";
import { getBalanceSheet, type BalanceSheet as BalanceSheetData, type BalanceSheetLine } from "../../../helpers/api/reports";
import { formatCurrency } from "../../../utils/currency";

const today = () => new Date().toISOString().slice(0, 10);

const Section = ({ title, lines, total }: { title: string; lines: BalanceSheetLine[]; total: number }) => (
  <div className="mb-6">
    <h5 className="mb-2 font-medium text-slate-900 dark:text-slate-200">{title}</h5>
    <table className="w-full text-sm">
      <tbody>
        {lines.length === 0 && (
          <tr><td className="py-1 text-slate-400">No accounts with activity.</td></tr>
        )}
        {lines.map((l) => (
          <tr key={l.accountCode} className="border-b border-slate-100 dark:border-slate-800">
            <td className="py-1.5">{l.accountCode} {l.accountName}</td>
            <td className="py-1.5 text-right">{formatCurrency(l.balance)}</td>
          </tr>
        ))}
        <tr className="font-medium border-t border-slate-300 dark:border-slate-600">
          <td className="py-1.5">Total {title}</td>
          <td className="py-1.5 text-right">{formatCurrency(total)}</td>
        </tr>
      </tbody>
    </table>
  </div>
);

const BalanceSheet = () => {
  const company = useCompanyProfile();
  const [asOfDate, setAsOfDate] = useState(today());
  const [data, setData] = useState<BalanceSheetData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getBalanceSheet(asOfDate)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [asOfDate]);

  const isBalanced = data && data.totalAssets === data.totalLiabilitiesAndEquity;
  const totalLiabilities = data?.liabilities.reduce((s, l) => s + l.balance, 0) ?? 0;
  const totalEquity = data?.equity.reduce((s, l) => s + l.balance, 0) ?? 0;

  return (
    <>
      <PageBreadcrumb title="Balance Sheet" name="Balance Sheet" breadCrumbItems={["Accounting", "Reports", "Balance Sheet"]}>
        <input type="date" value={asOfDate} onChange={(e) => setAsOfDate(e.target.value)} className="form-input text-sm" />
      </PageBreadcrumb>

      <ReportLetterhead company={company} />

      {loading && <div className="card"><div className="card-body text-sm text-slate-400">Loading…</div></div>}

      {!loading && data && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          <div className="card">
            <div className="card-body">
              <Section title="Assets" lines={data.assets} total={data.totalAssets} />
            </div>
          </div>
          <div className="card">
            <div className="card-body">
              <Section title="Liabilities" lines={data.liabilities} total={totalLiabilities} />
              <Section title="Equity" lines={data.equity} total={totalEquity} />
              <div className="flex justify-between text-sm border-t border-slate-200 dark:border-slate-700 pt-2">
                <span>Current Year Earnings</span>
                <span>{formatCurrency(data.currentYearEarnings)}</span>
              </div>
              <div className="flex justify-between font-medium border-t border-slate-300 dark:border-slate-600 pt-2 mt-2">
                <span>Total Liabilities &amp; Equity</span>
                <span>{formatCurrency(data.totalLiabilitiesAndEquity)}</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {!loading && data && (
        <p className={`mt-3 text-sm font-medium ${isBalanced ? "text-emerald-600" : "text-red-600"}`}>
          {isBalanced ? "✓ Balanced" : "⚠ Assets do not equal Liabilities + Equity"}
        </p>
      )}
    </>
  );
};

export default BalanceSheet;
