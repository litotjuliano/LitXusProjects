import { useEffect, useState } from "react";
import { useSelector } from "react-redux";

import { PageBreadcrumb } from "../../components";
import { RootState } from "../../redux/store";
import { listAccounts, listGLEntries, type Account, type GLEntry } from "../../helpers/api/accounting";

const Dashboard = () => {
  const user = useSelector((state: RootState) => state.Auth.user as any);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [glEntries, setGlEntries] = useState<GLEntry[]>([]);

  useEffect(() => {
    listAccounts().then((res) => setAccounts(res.data?.data ?? [])).catch(() => setAccounts([]));
    listGLEntries().then((res) => setGlEntries(res.data?.data ?? [])).catch(() => setGlEntries([]));
  }, []);

  const cashBalance = accounts
    .filter((a) => a.code.startsWith("10"))
    .reduce((sum, a) => sum + a.balance, 0);
  const draftCount = glEntries.filter((e) => e.status === "Draft").length;
  const recentEntries = glEntries.slice(0, 5);

  return (
    <>
      <PageBreadcrumb title="Dashboard" name="Dashboard" breadCrumbItems={["Accounting", "Dashboard"]} />
      <p className="text-sm text-slate-500 dark:text-slate-400 -mt-4 mb-6">
        Welcome back, {user?.fullName ?? user?.email ?? "there"}.
      </p>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-5">
        <div className="card">
          <div className="card-body">
            <p className="text-xs text-slate-500 dark:text-slate-400">Cash on Hand</p>
            <p className="text-lg font-semibold text-slate-900 dark:text-slate-200">
              {cashBalance.toLocaleString("en-MY", { style: "currency", currency: "MYR" })}
            </p>
          </div>
        </div>
        <div className="card">
          <div className="card-body">
            <p className="text-xs text-slate-500 dark:text-slate-400">Draft GL Entries</p>
            <p className="text-lg font-semibold text-slate-900 dark:text-slate-200">{draftCount}</p>
          </div>
        </div>
        <div className="card">
          <div className="card-body">
            <p className="text-xs text-slate-500 dark:text-slate-400">Chart of Accounts</p>
            <p className="text-lg font-semibold text-slate-900 dark:text-slate-200">{accounts.length} accounts</p>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h5 className="font-medium text-slate-900 dark:text-slate-200">Recent GL Entries</h5>
        </div>
        <div className="card-body p-0">
          <ul className="divide-y divide-slate-100 dark:divide-slate-800">
            {recentEntries.length === 0 && (
              <li className="px-4 py-6 text-center text-sm text-slate-400">No entries yet.</li>
            )}
            {recentEntries.map((e) => (
              <li key={e.id} className="flex items-center justify-between px-4 py-3 text-sm">
                <span>{e.entryNumber ?? "(Draft)"}</span>
                <span className="text-slate-500 dark:text-slate-400">{e.entryDate}</span>
                <span className="text-slate-500 dark:text-slate-400">{e.status}</span>
                <span className="text-slate-700 dark:text-slate-300">{e.description}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </>
  );
};

export default Dashboard;
