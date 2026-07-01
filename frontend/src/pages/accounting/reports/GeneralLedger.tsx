import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../../components";
import { getGeneralLedger, type GeneralLedger as GeneralLedgerData } from "../../../helpers/api/reports";
import { listAccounts, type Account } from "../../../helpers/api/accounting";
import { formatCurrency } from "../../../utils/currency";

const startOfYear = () => `${new Date().getFullYear()}-01-01`;
const today = () => new Date().toISOString().slice(0, 10);

const GeneralLedger = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [accountId, setAccountId] = useState("");
  const [from, setFrom] = useState(startOfYear());
  const [to, setTo] = useState(today());
  const [data, setData] = useState<GeneralLedgerData | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    listAccounts()
      .then((res) => {
        const list: Account[] = res.data?.data ?? [];
        setAccounts(list);
        if (list.length > 0) setAccountId(list[0].id);
      })
      .catch(() => setAccounts([]));
  }, []);

  useEffect(() => {
    if (!accountId) return;
    setLoading(true);
    getGeneralLedger(accountId, from, to)
      .then((res) => setData(res.data?.data ?? null))
      .catch(() => setData(null))
      .finally(() => setLoading(false));
  }, [accountId, from, to]);

  return (
    <>
      <PageBreadcrumb title="General Ledger" name="General Ledger" breadCrumbItems={["Accounting", "Reports", "General Ledger"]}>
        <div className="flex items-center gap-2">
          <select value={accountId} onChange={(e) => setAccountId(e.target.value)} className="form-select text-sm">
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>{a.code} {a.name}</option>
            ))}
          </select>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="form-input text-sm" />
          <span className="text-slate-400 text-sm">to</span>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="form-input text-sm" />
        </div>
      </PageBreadcrumb>

      {accounts.length === 0 && (
        <div className="card"><div className="card-body text-sm text-slate-400">No accounts yet — create one in Chart of Accounts first.</div></div>
      )}

      {accounts.length > 0 && (
        <div className="card">
          <div className="card-body p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Entry #</th>
                  <th className="px-4 py-3 font-medium">Description</th>
                  <th className="px-4 py-3 font-medium text-right">Debit</th>
                  <th className="px-4 py-3 font-medium text-right">Credit</th>
                  <th className="px-4 py-3 font-medium text-right">Balance</th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr><td colSpan={6} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
                )}
                {!loading && data?.lines.length === 0 && (
                  <tr><td colSpan={6} className="px-4 py-6 text-center text-slate-400">No posted activity in this period.</td></tr>
                )}
                {!loading && data?.lines.map((l) => (
                  <tr key={l.glEntryId} className="border-b border-slate-100 dark:border-slate-800">
                    <td className="px-4 py-2">{l.entryDate}</td>
                    <td className="px-4 py-2">{l.entryNumber ?? "—"}</td>
                    <td className="px-4 py-2">{l.description}</td>
                    <td className="px-4 py-2 text-right">{l.debit ? formatCurrency(l.debit) : ""}</td>
                    <td className="px-4 py-2 text-right">{l.credit ? formatCurrency(l.credit) : ""}</td>
                    <td className="px-4 py-2 text-right font-medium">{formatCurrency(l.runningBalance)}</td>
                  </tr>
                ))}
              </tbody>
              {!loading && data && (
                <tfoot>
                  <tr className="border-t-2 border-slate-300 dark:border-slate-600 font-medium">
                    <td className="px-4 py-3" colSpan={5}>Ending Balance</td>
                    <td className="px-4 py-3 text-right">{formatCurrency(data.endingBalance)}</td>
                  </tr>
                </tfoot>
              )}
            </table>
          </div>
        </div>
      )}
    </>
  );
};

export default GeneralLedger;
