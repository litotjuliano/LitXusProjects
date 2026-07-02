import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listAccounts,
  createAccount,
  type Account,
  type AccountType,
} from "../../helpers/api/accounting";

interface AccountFormValues {
  code: string;
  name: string;
  type: AccountType;
}

const ACCOUNT_TYPES: AccountType[] = ["Asset", "Liability", "Equity", "Revenue", "Expense"];

const ChartOfAccounts = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const fetchAccounts = () => {
    setLoading(true);
    listAccounts()
      .then((res) => setAccounts(res.data?.data ?? []))
      .catch(() => setAccounts([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchAccounts, []);

  const schemaResolver = yupResolver<AccountFormValues>(
    yup.object().shape({
      code: yup.string().required("Please enter an account code"),
      name: yup.string().required("Please enter an account name"),
      type: yup.mixed<AccountType>().oneOf(ACCOUNT_TYPES).required("Please select an account type"),
    })
  );

  const columns: DataTableColumn<Account>[] = [
    { key: "code", header: "Code", render: (a) => a.code, sortAccessor: (a) => a.code },
    { key: "name", header: "Name", render: (a) => a.name, sortAccessor: (a) => a.name },
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
  ];

  const onSubmit = async (formData: AccountFormValues) => {
    setFormError(null);
    try {
      await createAccount({ ...formData, parentAccountId: null });
      setShowModal(false);
      fetchAccounts();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not create the account. Please try again.");
    }
  };

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

      <DataTable<Account>
        columns={columns}
        data={accounts}
        rowKey={(a) => a.id}
        loading={loading}
        emptyMessage="No accounts yet. Create your first account to get started."
        searchPlaceholder="Search by code, name, or type…"
        searchAccessor={(a) => `${a.code} ${a.name} ${a.type}`}
      />

      <ModalLayout showModal={showModal} toggleModal={() => setShowModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Account</h5>
        <VerticalForm<AccountFormValues> onSubmit={onSubmit} resolver={schemaResolver}>
          <FormInput
            label="Code"
            type="text"
            name="code"
            placeholder="1010"
            containerClass="mb-4"
            className="form-input"
            labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2"
          />
          <FormInput
            label="Name"
            type="text"
            name="name"
            placeholder="Cash - Maybank Current"
            containerClass="mb-4"
            className="form-input"
            labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2"
          />
          <FormInput
            label="Type"
            type="select"
            name="type"
            containerClass="mb-4"
            className="form-select"
            labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2"
          >
            {ACCOUNT_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
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
    </>
  );
};

export default ChartOfAccounts;
