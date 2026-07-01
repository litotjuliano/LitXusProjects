import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../components";
import { listAuditLogs, type AuditLog } from "../../helpers/api/admin";

const actionStyles: Record<string, string> = {
  Create: "text-emerald-600",
  Update: "text-slate-500",
  Delete: "text-red-500",
  Approve: "text-emerald-600",
  Void: "text-amber-600",
  Activate: "text-emerald-600",
  Deactivate: "text-red-500",
  AssignRole: "text-emerald-600",
  RevokeRole: "text-red-500",
};

const AuditLogs = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  useEffect(() => {
    listAuditLogs()
      .then((res) => setLogs(res.data?.data ?? []))
      .catch(() => setLogs([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      <PageBreadcrumb title="Audit Logs" name="Audit Logs" breadCrumbItems={["Administration", "Audit Logs"]} />
      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Time</th>
                <th className="px-4 py-3 font-medium">User</th>
                <th className="px-4 py-3 font-medium">Action</th>
                <th className="px-4 py-3 font-medium">Entity</th>
                <th className="px-4 py-3 font-medium">Reason</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && logs.length === 0 && (
                <tr><td colSpan={5} className="px-4 py-6 text-center text-slate-400">No audit activity yet.</td></tr>
              )}
              {!loading && logs.map((log) => {
                const isExpanded = expandedId === log.id;
                return (
                  <>
                    <tr
                      key={log.id}
                      onClick={() => setExpandedId(isExpanded ? null : log.id)}
                      className="cursor-pointer border-b border-slate-100 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800"
                    >
                      <td className="px-4 py-2 text-slate-500 dark:text-slate-400 whitespace-nowrap">
                        {new Date(log.timestampUtc).toLocaleString()}
                      </td>
                      <td className="px-4 py-2">{log.userEmail ?? "—"}</td>
                      <td className="px-4 py-2">
                        <span className={actionStyles[log.action] ?? "text-slate-500"}>{log.action}</span>
                      </td>
                      <td className="px-4 py-2">
                        {log.entityName} <span className="text-slate-400 font-mono text-xs">{log.entityId.slice(0, 8)}</span>
                      </td>
                      <td className="px-4 py-2 text-slate-500 dark:text-slate-400">{log.reason ?? "—"}</td>
                    </tr>
                    {isExpanded && (
                      <tr className="border-b border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-900">
                        <td colSpan={5} className="px-4 py-3">
                          <div className="grid grid-cols-2 gap-4 text-xs">
                            <div>
                              <p className="mb-1 font-medium text-slate-500">Before</p>
                              <pre className="whitespace-pre-wrap rounded bg-white dark:bg-slate-800 p-2 font-mono">
                                {log.beforeValuesJson ? JSON.stringify(JSON.parse(log.beforeValuesJson), null, 2) : "—"}
                              </pre>
                            </div>
                            <div>
                              <p className="mb-1 font-medium text-slate-500">After</p>
                              <pre className="whitespace-pre-wrap rounded bg-white dark:bg-slate-800 p-2 font-mono">
                                {log.afterValuesJson ? JSON.stringify(JSON.parse(log.afterValuesJson), null, 2) : "—"}
                              </pre>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
};

export default AuditLogs;
