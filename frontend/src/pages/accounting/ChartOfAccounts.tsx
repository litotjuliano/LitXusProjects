import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm } from "../../components";
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

  const onSubmit = async (formData: AccountFormValues) => {
    await createAccount({ ...formData, parentAccountId: null });
    setShowModal(false);
    fetchAccounts();
  };

  return (
    <>
      <PageBreadcrumb title="Chart of Accounts" name="Chart of Accounts" breadCrumbItems={["Accounting", "Chart of Accounts"]}>
        <button
          className="btn text-white bg-primary text-sm"
          onClick={() => setShowModal(true)}
        >
          + New Account
        </button>
      </PageBreadcrumb>

      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Code</th>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Type</th>
                <th className="px-4 py-3 font-medium text-right">Balance</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-slate-400">Loading…</td>
                </tr>
              )}
              {!loading && accounts.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-slate-400">
                    No accounts yet. Create your first account to get started.
                  </td>
                </tr>
              )}
              {!loading && accounts.map((a) => (
                <tr key={a.id} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{a.code}</td>
                  <td className="px-4 py-2">{a.name}</td>
                  <td className="px-4 py-2">{a.type}</td>
                  <td className="px-4 py-2 text-right">{a.balance.toFixed(2)}</td>
                  <td className="px-4 py-2">
                    <span className={a.isActive ? "text-emerald-600" : "text-slate-400"}>
                      {a.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

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
