import { useEffect, useMemo, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listAccounts,
  createAccount,
  updateAccount,
  deactivateAccount,
  reactivateAccount,
  type Account,
  type AccountType,
} from "../../helpers/api/accounting";

interface AccountFormValues {
  code: string;
  name: string;
  type: AccountType;
  parentAccountId: string;
}

interface EditAccountFormValues {
  name: string;
  parentAccountId: string;
}

const ACCOUNT_TYPES: AccountType[] = ["Asset", "Liability", "Equity", "Revenue", "Expense"];

/** Orders accounts as a parent/child tree (roots first, each followed immediately by its
 * descendants) with a depth per account for indentation — rather than the flat code order the
 * API returns. Orphaned parentAccountId references (shouldn't happen, but defensive) are treated
 * as roots so nothing silently disappears from the list. */
function buildTree(accounts: Account[]): { account: Account; depth: number }[] {
  const byParent = new Map<string | null, Account[]>();
  const knownIds = new Set(accounts.map((a) => a.id));
  for (const a of accounts) {
    const parentKey = a.parentAccountId && knownIds.has(a.parentAccountId) ? a.parentAccountId : null;
    if (!byParent.has(parentKey)) byParent.set(parentKey, []);
    byParent.get(parentKey)!.push(a);
  }
  for (const list of byParent.values()) list.sort((a, b) => a.code.localeCompare(b.code));

  const result: { account: Account; depth: number }[] = [];
  const walk = (parentId: string | null, depth: number) => {
    for (const account of byParent.get(parentId) ?? []) {
      result.push({ account, depth });
      walk(account.id, depth + 1);
    }
  };
  walk(null, 0);
  return result;
}

/** IDs of an account and everything beneath it — excluded from its own "Parent Account" options
 * so reparenting can't create a cycle (the backend also rejects this, this is just so the
 * dropdown doesn't offer an obviously-invalid choice in the first place). */
function descendantIds(accounts: Account[], rootId: string): Set<string> {
  const children = new Map<string, string[]>();
  for (const a of accounts) {
    if (!a.parentAccountId) continue;
    if (!children.has(a.parentAccountId)) children.set(a.parentAccountId, []);
    children.get(a.parentAccountId)!.push(a.id);
  }
  const result = new Set<string>([rootId]);
  const stack = [rootId];
  while (stack.length) {
    const id = stack.pop()!;
    for (const childId of children.get(id) ?? []) {
      if (!result.has(childId)) {
        result.add(childId);
        stack.push(childId);
      }
    }
  }
  return result;
}

const fieldClass = "form-input w-full";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";

const ChartOfAccounts = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [showInactive, setShowInactive] = useState(false);

  const fetchAccounts = () => {
    setLoading(true);
    listAccounts(showInactive)
      .then((res) => setAccounts(res.data?.data ?? []))
      .catch(() => setAccounts([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchAccounts, [showInactive]);

  const tree = useMemo(() => buildTree(accounts), [accounts]);
  const orderedAccounts = useMemo(() => tree.map((t) => t.account), [tree]);
  const depthById = useMemo(() => new Map(tree.map((t) => [t.account.id, t.depth])), [tree]);

  const schemaResolver = yupResolver<AccountFormValues>(
    yup.object().shape({
      code: yup.string().required("Please enter an account code"),
      name: yup.string().required("Please enter an account name"),
      type: yup.mixed<AccountType>().oneOf(ACCOUNT_TYPES).required("Please select an account type"),
      parentAccountId: yup.string().default(""),
    })
  );

  const editSchemaResolver = yupResolver<EditAccountFormValues>(
    yup.object().shape({
      name: yup.string().required("Please enter an account name"),
      parentAccountId: yup.string().default(""),
    })
  );

  const onEditSubmit = async (formData: EditAccountFormValues) => {
    if (!editingAccount) return;
    setEditError(null);
    try {
      await updateAccount(editingAccount.id, formData.name, formData.parentAccountId || null);
      setEditingAccount(null);
      fetchAccounts();
    } catch (err) {
      setEditError(typeof err === "string" ? err : "Could not update the account. Please try again.");
    }
  };

  const handleDeactivate = async (account: Account) => {
    setBusyId(account.id);
    try {
      await deactivateAccount(account.id);
      await fetchAccounts();
    } finally {
      setBusyId(null);
    }
  };

  const handleReactivate = async (account: Account) => {
    setBusyId(account.id);
    try {
      await reactivateAccount(account.id);
      await fetchAccounts();
    } finally {
      setBusyId(null);
    }
  };

  const columns: DataTableColumn<Account>[] = [
    { key: "code", header: "Code", render: (a) => a.code, sortAccessor: (a) => a.code },
    {
      key: "name",
      header: "Name",
      // No sortAccessor here on purpose — the default (unsorted) view preserves the parent/child
      // tree order from `orderedAccounts`. Sorting by another column still works and simply
      // flattens the hierarchy, which is expected.
      render: (a) => (
        <span style={{ paddingLeft: (depthById.get(a.id) ?? 0) * 20 }}>
          {(depthById.get(a.id) ?? 0) > 0 && <span className="text-slate-300 dark:text-slate-600 mr-1">└</span>}
          {a.name}
        </span>
      ),
    },
    { key: "type", header: "Type", render: (a) => a.type, sortAccessor: (a) => a.type },
    {
      key: "balance",
      header: "Balance",
      className: "text-right",
      headerClassName: "text-right",
      render: (a) => a.balance.toFixed(2),
      sortAccessor: (a) => a.balance,
    },
    {
      key: "status",
      header: "Status",
      render: (a) => (
        <span className={a.isActive ? "text-emerald-600" : "text-slate-400"}>
          {a.isActive ? "Active" : "Inactive"}
        </span>
      ),
      sortAccessor: (a) => (a.isActive ? 1 : 0),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (a) => (
        <div className="flex justify-end gap-3">
          {a.isActive && (
            <button
              onClick={() => { setEditError(null); setEditingAccount(a); }}
              className="text-xs font-medium text-primary hover:underline"
            >
              Edit
            </button>
          )}
          {a.isActive ? (
            <button
              onClick={() => handleDeactivate(a)}
              disabled={busyId === a.id}
              className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50"
            >
              Deactivate
            </button>
          ) : (
            <button
              onClick={() => handleReactivate(a)}
              disabled={busyId === a.id}
              className="text-xs font-medium text-emerald-600 hover:underline disabled:opacity-50"
            >
              Reactivate
            </button>
          )}
        </div>
      ),
    },
  ];

  const onSubmit = async (formData: AccountFormValues) => {
    setFormError(null);
    try {
      await createAccount({ ...formData, parentAccountId: formData.parentAccountId || null });
      setShowModal(false);
      fetchAccounts();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not create the account. Please try again.");
    }
  };

  const editParentOptions = editingAccount
    ? accounts.filter((a) => a.isActive && !descendantIds(accounts, editingAccount.id).has(a.id))
    : [];

  return (
    <>
      <PageBreadcrumb title="Chart of Accounts" name="Chart of Accounts" breadCrumbItems={["Accounting", "Chart of Accounts"]}>
        <button
          className="btn text-white bg-primary text-sm"
          onClick={() => {
            setFormError(null);
            setShowModal(true);
          }}
        >
          + New Account
        </button>
      </PageBreadcrumb>

      <label className="flex items-center gap-2 mb-3 text-sm text-slate-600 dark:text-slate-300">
        <input
          type="checkbox"
          checked={showInactive}
          onChange={(e) => setShowInactive(e.target.checked)}
          className="form-checkbox rounded"
        />
        Show inactive accounts
      </label>

      <DataTable<Account>
        columns={columns}
        data={orderedAccounts}
        rowKey={(a) => a.id}
        loading={loading}
        emptyMessage="No accounts yet. Create your first account to get started."
        searchPlaceholder="Search by code, name, or type…"
        searchAccessor={(a) => `${a.code} ${a.name} ${a.type}`}
      />

      <ModalLayout showModal={showModal} toggleModal={() => setShowModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Account</h5>
        <VerticalForm<AccountFormValues> onSubmit={onSubmit} resolver={schemaResolver} defaultValues={{ parentAccountId: "" }}>
          <FormInput
            label="Code"
            type="text"
            name="code"
            placeholder="1010"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Name"
            type="text"
            name="name"
            placeholder="Cash - Maybank Current"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Type"
            type="select"
            name="type"
            containerClass="mb-4"
            className="form-select"
            labelClassName={labelClass}
          >
            {ACCOUNT_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </FormInput>
          <FormInput
            label="Parent Account (optional)"
            type="select"
            name="parentAccountId"
            containerClass="mb-4"
            className="form-select"
            labelClassName={labelClass}
          >
            <option value="">None — top-level account</option>
            {accounts.filter((a) => a.isActive).map((a) => (
              <option key={a.id} value={a.id}>{a.code} {a.name}</option>
            ))}
          </FormInput>
          <div className="text-sm text-red-600 mb-3">{formError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowModal(false)}>
              Cancel
            </button>
            <button type="submit" className="btn text-white bg-primary text-sm">
              Create Account
            </button>
          </div>
        </VerticalForm>
      </ModalLayout>

      <ModalLayout showModal={editingAccount !== null} toggleModal={() => setEditingAccount(null)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">Edit Account</h5>
        {editingAccount && (
          <VerticalForm<EditAccountFormValues>
            key={editingAccount.id}
            onSubmit={onEditSubmit}
            resolver={editSchemaResolver}
            defaultValues={{ name: editingAccount.name, parentAccountId: editingAccount.parentAccountId ?? "" }}
          >
            <div className="mb-4 text-sm text-slate-500 dark:text-slate-400">
              Code <span className="font-mono">{editingAccount.code}</span> and Type{" "}
              <span className="font-medium">{editingAccount.type}</span> can't be changed here.
            </div>
            <FormInput
              label="Name"
              type="text"
              name="name"
              containerClass="mb-4"
              className={fieldClass}
              labelClassName={labelClass}
            />
            <FormInput
              label="Parent Account (optional)"
              type="select"
              name="parentAccountId"
              containerClass="mb-4"
              className="form-select"
              labelClassName={labelClass}
            >
              <option value="">None — top-level account</option>
              {editParentOptions.map((a) => (
                <option key={a.id} value={a.id}>{a.code} {a.name}</option>
              ))}
            </FormInput>
            <div className="text-sm text-red-600 mb-3">{editError}</div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setEditingAccount(null)}>
                Cancel
              </button>
              <button type="submit" className="btn text-white bg-primary text-sm">
                Save Changes
              </button>
            </div>
          </VerticalForm>
        )}
      </ModalLayout>
    </>
  );
};

export default ChartOfAccounts;
