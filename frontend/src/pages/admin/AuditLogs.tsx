import { PageBreadcrumb } from "../../components";

const AuditLogs = () => {
  return (
    <>
      <PageBreadcrumb title="Audit Logs" name="Audit Logs" breadCrumbItems={["Administration", "Audit Logs"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Audit log viewer — wired to GET /admin/audit-logs once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default AuditLogs;
