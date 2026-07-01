import { useEffect, useState } from "react";
import { PageBreadcrumb } from "../../components";
import { listUsers, updateUserStatus, type UserSummary } from "../../helpers/api/admin";

const Users = () => {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);

  const fetchUsers = () => {
    setLoading(true);
    listUsers()
      .then((res) => setUsers(res.data?.data ?? []))
      .catch(() => setUsers([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchUsers, []);

  const toggleStatus = async (user: UserSummary) => {
    setBusyId(user.id);
    try {
      await updateUserStatus(user.id, !user.isActive);
      await fetchUsers();
    } finally {
      setBusyId(null);
    }
  };

  return (
    <>
      <PageBreadcrumb title="Users" name="Users" breadCrumbItems={["Administration", "Users"]} />
      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Email</th>
                <th className="px-4 py-3 font-medium">Roles</th>
                <th className="px-4 py-3 font-medium">Last Login</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium" />
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={6} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && users.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-6 text-center text-slate-400">No users yet.</td></tr>
              )}
              {!loading && users.map((u) => (
                <tr key={u.id} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{u.fullName}</td>
                  <td className="px-4 py-2 text-slate-500 dark:text-slate-400">{u.email}</td>
                  <td className="px-4 py-2">
                    <div className="flex flex-wrap gap-1">
                      {u.roles.length === 0 && <span className="text-slate-400">—</span>}
                      {u.roles.map((r) => (
                        <span key={r} className="rounded bg-slate-100 dark:bg-slate-700 px-2 py-0.5 text-xs">{r}</span>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-2 text-slate-500 dark:text-slate-400">
                    {u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc).toLocaleString() : "Never"}
                  </td>
                  <td className="px-4 py-2">
                    <span className={u.isActive ? "text-emerald-600" : "text-slate-400"}>
                      {u.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-right">
                    <button
                      onClick={() => toggleStatus(u)}
                      disabled={busyId === u.id}
                      className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
                    >
                      {u.isActive ? "Deactivate" : "Activate"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
};

export default Users;
