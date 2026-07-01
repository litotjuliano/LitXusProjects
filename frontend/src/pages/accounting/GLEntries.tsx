import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";

import { PageBreadcrumb } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listAccounts,
  listGLEntries,
  createGLEntry,
  postGLEntry,
  type Account,
  type GLEntry,
  type GLEntryStatus,
} from "../../helpers/api/accounting";

interface GLEntryFormValues {
  entryDate: string;
  description: string;
  lines: { accountId: string; debitAmount: number; creditAmount: number; lineDescription?: string }[];
}

const statusStyles: Record<GLEntryStatus, string> = {
  Draft: "text-slate-500",
  Posted: "text-emerald-600",
  Voided: "text-red-500 line-through",
};

const GLEntries = () => {
  const [entries, setEntries] = useState<GLEntry[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);

  const fetchEntries = () => {
    setLoading(true);
    listGLEntries()
      .then((res) => setEntries(res.data?.data ?? []))
      .catch(() => setEntries([]))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchEntries();
    listAccounts().then((res) => setAccounts(res.data?.data ?? [])).catch(() => setAccounts([]));
  }, []);

  const { register, control, handleSubmit, watch, reset } = useForm<GLEntryFormValues>({
    defaultValues: {
      entryDate: new Date().toISOString().slice(0, 10),
      description: "",
      lines: [
        { accountId: "", debitAmount: 0, creditAmount: 0 },
        { accountId: "", debitAmount: 0, creditAmount: 0 },
      ],
    },
  });
  const { fields, append, remove } = useFieldArray({ control, name: "lines" });
  const lines = watch("lines");
  const totalDebit = lines.reduce((sum, l) => sum + (Number(l.debitAmount) || 0), 0);
  const totalCredit = lines.reduce((sum, l) => sum + (Number(l.creditAmount) || 0), 0);
  const isBalanced = totalDebit === totalCredit && totalDebit > 0 && lines.length >= 2;

  const closeModal = () => {
    setShowModal(false);
    reset();
  };

  const submit = async (values: GLEntryFormValues, post: boolean) => {
    const entry = await createGLEntry(values).then((res) => res.data.data);
    if (post) await postGLEntry(entry.id);
    closeModal();
    fetchEntries();
  };

  return (
    <>
      <PageBreadcrumb title="GL Entries" name="GL Entries" breadCrumbItems={["Accounting", "GL Entries"]}>
        <button className="btn text-white bg-primary text-sm" onClick={() => setShowModal(true)}>
          + New Entry
        </button>
      </PageBreadcrumb>

      <div className="card">
        <div className="card-body p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <th className="px-4 py-3 font-medium">Number</th>
                <th className="px-4 py-3 font-medium">Date</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Description</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>
              )}
              {!loading && entries.length === 0 && (
                <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">No GL entries yet.</td></tr>
              )}
              {!loading && entries.map((e) => (
                <tr key={e.id} className="border-b border-slate-100 dark:border-slate-800">
                  <td className="px-4 py-2">{e.entryNumber ?? "(Draft)"}</td>
                  <td className="px-4 py-2">{e.entryDate}</td>
                  <td className="px-4 py-2"><span className={statusStyles[e.status]}>{e.status}</span></td>
                  <td className="px-4 py-2">{e.description}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <ModalLayout showModal={showModal} toggleModal={closeModal} panelClassName="w-full max-w-3xl bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New GL Entry</h5>
        <form className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Entry Date</label>
              <input type="date" {...register("entryDate", { required: true })} className="form-input w-full" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Description</label>
              <input type="text" {...register("description", { required: true })} className="form-input w-full" />
            </div>
          </div>

          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-slate-500 dark:text-slate-400">
                <th className="pb-2 font-medium">Account</th>
                <th className="pb-2 font-medium">Description</th>
                <th className="w-28 pb-2 text-right font-medium">Debit</th>
                <th className="w-28 pb-2 text-right font-medium">Credit</th>
                <th className="w-8" />
              </tr>
            </thead>
            <tbody>
              {fields.map((field, index) => (
                <tr key={field.id}>
                  <td className="pr-2 py-1">
                    <select {...register(`lines.${index}.accountId`, { required: true })} className="form-select w-full">
                      <option value="">Select account…</option>
                      {accounts.filter((a) => a.isActive).map((a) => (
                        <option key={a.id} value={a.id}>{a.code} {a.name}</option>
                      ))}
                    </select>
                  </td>
                  <td className="pr-2 py-1">
                    <input {...register(`lines.${index}.lineDescription`)} className="form-input w-full" />
                  </td>
                  <td className="pr-2 py-1">
                    <input type="number" step="0.01" {...register(`lines.${index}.debitAmount`, { valueAsNumber: true })} className="form-input w-full text-right" />
                  </td>
                  <td className="pr-2 py-1">
                    <input type="number" step="0.01" {...register(`lines.${index}.creditAmount`, { valueAsNumber: true })} className="form-input w-full text-right" />
                  </td>
                  <td className="py-1 text-center">
                    <button type="button" onClick={() => remove(index)} className="text-slate-400 hover:text-red-600" aria-label="Remove line">×</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <button
            type="button"
            onClick={() => append({ accountId: "", debitAmount: 0, creditAmount: 0 })}
            className="text-sm font-medium text-primary hover:underline"
          >
            + Add Line
          </button>

          <div className="flex items-center justify-between border-t border-slate-200 dark:border-slate-700 pt-3 text-sm">
            <span className={isBalanced ? "font-medium text-emerald-600" : "font-medium text-red-600"}>
              {isBalanced ? "✓ Balanced" : "Not balanced"}
            </span>
            <span className="font-mono">{totalDebit.toFixed(2)} / {totalCredit.toFixed(2)}</span>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={closeModal}>
              Cancel
            </button>
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={handleSubmit((v) => submit(v, false))}>
              Save as Draft
            </button>
            <button
              type="button"
              disabled={!isBalanced}
              className="btn text-white bg-primary text-sm disabled:opacity-50"
              onClick={handleSubmit((v) => submit(v, true))}
            >
              Save & Post
            </button>
          </div>
        </form>
      </ModalLayout>
    </>
  );
};

export default GLEntries;
