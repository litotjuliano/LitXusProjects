import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { Navigate } from "react-router-dom";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { PageBreadcrumb, DataTable, FormInput, VerticalForm, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listUsers,
  createUser,
  updateUserStatus,
  assignRole,
  revokeRole,
  resetUserPassword,
  listRoles,
  type UserSummary,
  type Role,
} from "../../helpers/api/admin";
import { RootState } from "../../redux/store";

interface NewUserFormValues {
  fullName: string;
  email: string;
  password: string;
  roleId: string;
}

interface ResetPasswordFormValues {
  newPassword: string;
}

const fieldClass = "form-input w-full";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";

const Users = () => {
  const currentUser = useSelector((state: RootState) => state.Auth.user as any);
  // Menu hides this page from non-Admins, but routes/index.tsx's `roles` field isn't actually
  // enforced by the router (see Routes.tsx) — this guard is what actually blocks direct URL
  // navigation for everyone else (same pattern as pages/admin/License.tsx).
  const isAdminOrSuperAdmin = (currentUser?.roles ?? []).some((r: string) => r === "Admin" || r === "Super Admin");

  const [users, setUsers] = useState<UserSummary[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [roleChangingId, setRoleChangingId] = useState<string | null>(null);

  const [showNewUserModal, setShowNewUserModal] = useState(false);
  const [newUserError, setNewUserError] = useState<string | null>(null);

  const [resetPasswordUser, setResetPasswordUser] = useState<UserSummary | null>(null);
  const [resetPasswordError, setResetPasswordError] = useState<string | null>(null);

  const fetchUsers = () => {
    setLoading(true);
    listUsers()
      .then((res) => setUsers(res.data?.data ?? []))
      .catch(() => setUsers([]))
      .finally(() => setLoading(false));
  };

  const fetchRoles = () => {
    listRoles()
      .then((res) => setRoles(res.data?.data ?? []))
      .catch(() => setRoles([]));
  };

  useEffect(() => {
    fetchUsers();
    fetchRoles();
  }, []);

  const roleIdByName = (name: string) => roles.find((r) => r.name === name)?.id;

  const toggleStatus = async (user: UserSummary) => {
    setBusyId(user.id);
    try {
      await updateUserStatus(user.id, !user.isActive);
      await fetchUsers();
    } finally {
      setBusyId(null);
    }
  };

  const changeRole = async (user: UserSummary, newRoleId: string) => {
    setRoleChangingId(user.id);
    try {
      const currentRoleName = user.roles[0];
      const currentRoleId = currentRoleName ? roleIdByName(currentRoleName) : undefined;
      if (currentRoleId) {
        await revokeRole(user.id, currentRoleId);
      }
      if (newRoleId) {
        await assignRole(user.id, newRoleId);
      }
      await fetchUsers();
    } finally {
      setRoleChangingId(null);
    }
  };

  const newUserSchemaResolver = yupResolver<NewUserFormValues>(
    yup.object().shape({
      fullName: yup.string().required("Please enter a full name"),
      email: yup.string().email("Must be a valid email").required("Please enter an email"),
      password: yup.string().min(10, "Must be at least 10 characters").required("Please enter a password"),
      roleId: yup.string().required("Please select a role"),
    })
  );

  const onCreateUser = async (values: NewUserFormValues) => {
    setNewUserError(null);
    try {
      await createUser(values.email, values.fullName, values.password, values.roleId);
      setShowNewUserModal(false);
      await fetchUsers();
    } catch (err) {
      setNewUserError(typeof err === "string" ? err : "Could not create the user. Please try again.");
    }
  };

  const resetPasswordSchemaResolver = yupResolver<ResetPasswordFormValues>(
    yup.object().shape({
      newPassword: yup.string().min(10, "Must be at least 10 characters").required("Please enter a new password"),
    })
  );

  const onResetPassword = async (values: ResetPasswordFormValues) => {
    if (!resetPasswordUser) return;
    setResetPasswordError(null);
    try {
      await resetUserPassword(resetPasswordUser.id, values.newPassword);
      setResetPasswordUser(null);
    } catch (err) {
      setResetPasswordError(typeof err === "string" ? err : "Could not reset the password. Please try again.");
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
      header: "Role",
      render: (u) => (
        <select
          className="form-select text-xs py-1"
          value={roleIdByName(u.roles[0] ?? "") ?? ""}
          disabled={roleChangingId === u.id}
          onChange={(e) => changeRole(u, e.target.value)}
        >
          <option value="">No role</option>
          {roles.map((r) => (
            <option key={r.id} value={r.id}>{r.name}</option>
          ))}
        </select>
      ),
      sortAccessor: (u) => u.roles[0] ?? "",
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
        <div className="flex justify-end gap-3">
          <button
            onClick={() => { setResetPasswordError(null); setResetPasswordUser(u); }}
            className="text-xs font-medium text-primary hover:underline"
          >
            Reset Password
          </button>
          <button
            onClick={() => toggleStatus(u)}
            disabled={busyId === u.id}
            className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
          >
            {u.isActive ? "Deactivate" : "Activate"}
          </button>
        </div>
      ),
    },
  ];

  if (!isAdminOrSuperAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <>
      <PageBreadcrumb title="Users" name="Users" breadCrumbItems={["Administration", "Users"]} />

      <div className="flex justify-end mb-3">
        <button
          className="btn text-white bg-primary text-sm"
          onClick={() => { setNewUserError(null); setShowNewUserModal(true); }}
        >
          + New User
        </button>
      </div>

      <DataTable<UserSummary>
        columns={columns}
        data={users}
        rowKey={(u) => u.id}
        loading={loading}
        emptyMessage="No users yet."
        searchPlaceholder="Search by name or email…"
        searchAccessor={(u) => `${u.fullName} ${u.email}`}
      />

      <ModalLayout showModal={showNewUserModal} toggleModal={() => setShowNewUserModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New User</h5>
        <VerticalForm<NewUserFormValues> onSubmit={onCreateUser} resolver={newUserSchemaResolver}>
          <FormInput label="Full Name" type="text" name="fullName" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Email" type="text" name="email" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Password" type="password" name="password" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Role" type="select" name="roleId" containerClass="mb-4" className="form-select" labelClassName={labelClass}>
            <option value="">Select a role…</option>
            {roles.map((r) => (
              <option key={r.id} value={r.id}>{r.name}</option>
            ))}
          </FormInput>
          <div className="text-sm text-red-600 mb-3">{newUserError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowNewUserModal(false)}>
              Cancel
            </button>
            <button type="submit" className="btn text-white bg-primary text-sm">
              Create User
            </button>
          </div>
        </VerticalForm>
      </ModalLayout>

      <ModalLayout showModal={resetPasswordUser !== null} toggleModal={() => setResetPasswordUser(null)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">Reset Password</h5>
        {resetPasswordUser && (
          <VerticalForm<ResetPasswordFormValues> key={resetPasswordUser.id} onSubmit={onResetPassword} resolver={resetPasswordSchemaResolver}>
            <p className="text-sm text-slate-500 dark:text-slate-400 mb-4">
              Set a new password for <span className="font-medium">{resetPasswordUser.fullName}</span> ({resetPasswordUser.email}).
              Share it with them directly — there's no email delivery yet.
            </p>
            <FormInput label="New Password" type="password" name="newPassword" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <div className="text-sm text-red-600 mb-3">{resetPasswordError}</div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setResetPasswordUser(null)}>
                Cancel
              </button>
              <button type="submit" className="btn text-white bg-primary text-sm">
                Reset Password
              </button>
            </div>
          </VerticalForm>
        )}
      </ModalLayout>
    </>
  );
};

export default Users;
