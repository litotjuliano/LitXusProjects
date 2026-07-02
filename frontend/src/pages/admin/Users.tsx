import { useEffect, useState } from "react";
import { PageBreadcrumb, DataTable, type DataTableColumn } from "../../components";
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

  const columns: DataTableColumn<UserSummary>[] = [
    { key: "fullName", header: "Name", render: (u) => u.fullName, sortAccessor: (u) => u.fullName },
    {
      key: "email",
      header: "Email",
      render: (u) => <span className="text-slate-500 dark:text-slate-400">{u.email}</span>,
      sortAccessor: (u) => u.email,
    },
    {
      key: "roles",
      header: "Roles",
      render: (u) => (
        <div className="flex flex-wrap gap-1">
          {u.roles.length === 0 && <span className="text-slate-400">—</span>}
          {u.roles.map((r) => (
            <span key={r} className="rounded bg-slate-100 dark:bg-slate-700 px-2 py-0.5 text-xs">{r}</span>
          ))}
        </div>
      ),
    },
    {
      key: "lastLogin",
      header: "Last Login",
      render: (u) => (
        <span className="text-slate-500 dark:text-slate-400">
          {u.lastLoginAtUtc ? new Date(u.lastLoginAtUtc).toLocaleString() : "Never"}
        </span>
      ),
      sortAccessor: (u) => u.lastLoginAtUtc ?? "",
    },
    {
      key: "status",
      header: "Status",
      render: (u) => (
        <span className={u.isActive ? "text-emerald-600" : "text-slate-400"}>
          {u.isActive ? "Active" : "Inactive"}
        </span>
      ),
      sortAccessor: (u) => (u.isActive ? 1 : 0),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (u) => (
        <button
          onClick={() => toggleStatus(u)}
          disabled={busyId === u.id}
          className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
        >
          {u.isActive ? "Deactivate" : "Activate"}
        </button>
      ),
    },
  ];

  return (
    <>
      <PageBreadcrumb title="Users" name="Users" breadCrumbItems={["Administration", "Users"]} />
      <DataTable<UserSummary>
        columns={columns}
        data={users}
        rowKey={(u) => u.id}
        loading={loading}
        emptyMessage="No users yet."
        searchPlaceholder="Search by name or email…"
        searchAccessor={(u) => `${u.fullName} ${u.email}`}
      />
    </>
  );
};

export default Users;
