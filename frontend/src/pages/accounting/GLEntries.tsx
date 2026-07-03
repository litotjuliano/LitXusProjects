import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";

import { PageBreadcrumb, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listAccounts,
  listGLEntries,
  createGLEntry,
  updateGLEntry,
  postGLEntry,
  voidGLEntry,
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
  const [formError, setFormError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editingEntry, setEditingEntry] = useState<GLEntry | null>(null);

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

  const handlePost = async (entry: GLEntry) => {
    setBusyId(entry.id);
    try {
      await postGLEntry(entry.id);
      await fetchEntries();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not post this entry. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

  const handleVoid = async (entry: GLEntry) => {
    const reason = window.prompt("Reason for voiding this entry:");
    if (!reason) return;
    setBusyId(entry.id);
    try {
      await voidGLEntry(entry.id, reason);
      await fetchEntries();
    } catch (err) {
      window.alert(typeof err === "string" ? err : "Could not void this entry. Please try again.");
    } finally {
      setBusyId(null);
    }
  };

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

  const columns: DataTableColumn<GLEntry>[] = [
    { key: "number", header: "Number", render: (e) => e.entryNumber ?? "(Draft)", sortAccessor: (e) => e.entryNumber ?? "" },
    { key: "date", header: "Date", render: (e) => e.entryDate, sortAccessor: (e) => e.entryDate },
    {
      key: "status",
      header: "Status",
      render: (e) => <span className={statusStyles[e.status]}>{e.status}</span>,
      sortAccessor: (e) => e.status,
    },
    { key: "description", header: "Description", render: (e) => e.description, sortAccessor: (e) => e.description },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (e) => (
        <div className="flex justify-end gap-3">
          {e.status === "Draft" && (
            <>
              <button
                onClick={() => openEditModal(e)}
                className="text-xs font-medium text-primary hover:underline"
              >
                Edit
              </button>
              <button
                onClick={() => handlePost(e)}
                disabled={busyId === e.id}
                className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
              >
                Post
              </button>
            </>
          )}
          {e.status === "Posted" && (
            <button
              onClick={() => handleVoid(e)}
              disabled={busyId === e.id}
              className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50"
            >
              Void
            </button>
          )}
        </div>
      ),
    },
  ];

  const closeModal = () => {
    setShowModal(false);
    setFormError(null);
    setEditingEntry(null);
    reset();
  };

  const openNewModal = () => {
    setFormError(null);
    setEditingEntry(null);
    reset({
      entryDate: new Date().toISOString().slice(0, 10),
      description: "",
      lines: [
        { accountId: "", debitAmount: 0, creditAmount: 0 },
        { accountId: "", debitAmount: 0, creditAmount: 0 },
      ],
    });
    setShowModal(true);
  };

  const openEditModal = (entry: GLEntry) => {
    setFormError(null);
    setEditingEntry(entry);
    reset({
      entryDate: entry.entryDate,
      description: entry.description,
      lines: entry.lines.map((l) => ({
        accountId: l.accountId,
        debitAmount: l.debitAmount,
        creditAmount: l.creditAmount,
        lineDescription: l.lineDescription,
      })),
    });
    setShowModal(true);
  };

  const submit = async (values: GLEntryFormValues, post: boolean) => {
    setFormError(null);
    try {
      const entry = editingEntry
        ? await updateGLEntry(editingEntry.id, values).then((res) => res.data.data)
        : await createGLEntry(values).then((res) => res.data.data);
      if (post) await postGLEntry(entry.id);
      closeModal();
      fetchEntries();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not save the entry. Please try again.");
    }
  };

  return (
    <>
      <PageBreadcrumb title="GL Entries" name="GL Entries" breadCrumbItems={["Accounting", "GL Entries"]}>
        <button className="btn text-white bg-primary text-sm" onClick={openNewModal}>
          + New Entry
        </button>
      </PageBreadcrumb>

      <DataTable<GLEntry>
        columns={columns}
        data={entries}
        rowKey={(e) => e.id}
        loading={loading}
        emptyMessage="No GL entries yet."
        searchPlaceholder="Search by number or description…"
        searchAccessor={(e) => `${e.entryNumber ?? ""} ${e.description}`}
      />

      <ModalLayout showModal={showModal} toggleModal={closeModal} panelClassName="w-full max-w-3xl bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">
          {editingEntry ? "Edit GL Entry" : "New GL Entry"}
        </h5>
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

          {formError && (
            <div className="text-sm text-red-600">{formError}</div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={closeModal}>
              Cancel
            </button>
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={handleSubmit((v) => submit(v, false))}>
              {editingEntry ? "Save Changes" : "Save as Draft"}
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
