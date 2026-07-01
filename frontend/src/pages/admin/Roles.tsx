import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../components";
import { listRoles, type Role } from "../../helpers/api/admin";

const Roles = () => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  useEffect(() => {
    listRoles()
      .then((res) => setRoles(res.data?.data ?? []))
      .catch(() => setRoles([]))
      .finally(() => setLoading(false));
  }, []);

  return (
    <>
      <PageBreadcrumb title="Roles & Permissions" name="Roles & Permissions" breadCrumbItems={["Administration", "Roles"]} />

      {loading && (
        <div className="card"><div className="card-body text-sm text-slate-400">Loading…</div></div>
      )}

      {!loading && roles.length === 0 && (
        <div className="card"><div className="card-body text-sm text-slate-400">No roles found.</div></div>
      )}

      <div className="flex flex-col gap-3">
        {roles.map((role) => {
          const isExpanded = expandedId === role.id;
          return (
            <div key={role.id} className="card">
              <button
                type="button"
                onClick={() => setExpandedId(isExpanded ? null : role.id)}
                className="w-full text-left card-body flex items-center justify-between"
              >
                <div>
                  <h5 className="font-medium text-slate-900 dark:text-slate-200">{role.name}</h5>
                  {role.description && (
                    <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{role.description}</p>
                  )}
                </div>
                <span className="text-xs text-slate-400">
                  {role.permissions.length} permission{role.permissions.length === 1 ? "" : "s"}
                  {isExpanded ? " ▲" : " ▼"}
                </span>
              </button>
              {isExpanded && (
                <div className="border-t border-slate-100 dark:border-slate-800 px-6 py-4">
                  {role.permissions.length === 0 ? (
                    <p className="text-sm text-slate-400">No permissions granted.</p>
                  ) : (
                    <div className="flex flex-wrap gap-1.5">
                      {role.permissions.map((code) => (
                        <span key={code} className="rounded bg-slate-100 dark:bg-slate-700 px-2 py-1 text-xs font-mono">
                          {code}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </>
  );
};

export default Roles;
