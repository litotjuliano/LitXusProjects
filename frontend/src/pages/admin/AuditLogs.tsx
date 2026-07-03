import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { Navigate } from "react-router-dom";
import { PageBreadcrumb, DataTable, type DataTableColumn } from "../../components";
import { listAuditLogs, type AuditLog } from "../../helpers/api/admin";
import { RootState } from "../../redux/store";

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
  const currentUser = useSelector((state: RootState) => state.Auth.user as any);
  // Menu hides this page from non-Admins, but routes/index.tsx's `roles` field isn't actually
  // enforced by the router (see Routes.tsx) — this guard is what actually blocks direct URL
  // navigation for everyone else (same pattern as pages/admin/License.tsx).
  const isAdminOrSuperAdmin = (currentUser?.roles ?? []).some((r: string) => r === "Admin" || r === "Super Admin");

  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  useEffect(() => {
    listAuditLogs()
      .then((res) => setLogs(res.data?.data ?? []))
      .catch(() => setLogs([]))
      .finally(() => setLoading(false));
  }, []);

  const columns: DataTableColumn<AuditLog>[] = [
    {
      key: "time",
      header: "Time",
      className: "whitespace-nowrap",
      render: (log) => (
        <span className="text-slate-500 dark:text-slate-400 whitespace-nowrap">
          {new Date(log.timestampUtc).toLocaleString()}
        </span>
      ),
      sortAccessor: (log) => log.timestampUtc,
    },
    { key: "user", header: "User", render: (log) => log.userEmail ?? "—", sortAccessor: (log) => log.userEmail ?? "" },
    {
      key: "action",
      header: "Action",
      render: (log) => <span className={actionStyles[log.action] ?? "text-slate-500"}>{log.action}</span>,
      sortAccessor: (log) => log.action,
    },
    {
      key: "entity",
      header: "Entity",
      render: (log) => (
        <>
          {log.entityName} <span className="text-slate-400 font-mono text-xs">{log.entityId.slice(0, 8)}</span>
        </>
      ),
      sortAccessor: (log) => log.entityName,
    },
    {
      key: "reason",
      header: "Reason",
      render: (log) => <span className="text-slate-500 dark:text-slate-400">{log.reason ?? "—"}</span>,
    },
  ];

  if (!isAdminOrSuperAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <>
      <PageBreadcrumb title="Audit Logs" name="Audit Logs" breadCrumbItems={["Administration", "Audit Logs"]} />
      <DataTable<AuditLog>
        columns={columns}
        data={logs}
        rowKey={(log) => log.id}
        loading={loading}
        emptyMessage="No audit activity yet."
        searchPlaceholder="Search by user, action, entity, or reason…"
        searchAccessor={(log) => `${log.userEmail ?? ""} ${log.action} ${log.entityName} ${log.reason ?? ""}`}
        pageSize={15}
        onRowClick={(log) => setExpandedId(expandedId === log.id ? null : log.id)}
        isRowExpanded={(log) => expandedId === log.id}
        renderExpanded={(log) => (
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
        )}
      />
    </>
  );
};

export default AuditLogs;
