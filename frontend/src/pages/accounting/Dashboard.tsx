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
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Number</th>
                <th className="px-4 py-3 font-medium">Date</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Description</th>
              </tr>
            </thead>
            <tbody>
              {recentEntries.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-6 text-center text-slate-400">No entries yet.</td>
                </tr>
              )}
              {recentEntries.map((e) => (
                <tr key={e.id} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{e.entryNumber ?? "(Draft)"}</td>
                  <td className="px-4 py-2 text-slate-500 dark:text-slate-400">{e.entryDate}</td>
                  <td className="px-4 py-2 text-slate-500 dark:text-slate-400">{e.status}</td>
                  <td className="px-4 py-2 text-slate-700 dark:text-slate-300">{e.description}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
};

export default Dashboard;
