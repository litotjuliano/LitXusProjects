import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";

import { PageBreadcrumb } from "../../components";
import { getSalesSettings, configureSalesSettings } from "../../helpers/api/sales";
import { listAccounts, type Account } from "../../helpers/api/accounting";

interface SalesSettingsFormValues {
  receivableAccountId: string;
  revenueAccountId: string;
  taxPayableAccountId: string;
  cashAccountId: string;
}

const SalesSettingsPage = () => {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const { register, handleSubmit, reset } = useForm<SalesSettingsFormValues>({
    defaultValues: { receivableAccountId: "", revenueAccountId: "", taxPayableAccountId: "", cashAccountId: "" },
  });

  useEffect(() => {
    setLoading(true);
    Promise.all([getSalesSettings(), listAccounts()])
      .then(([settingsRes, accountsRes]) => {
        setAccounts(accountsRes.data?.data ?? []);
        const s = settingsRes.data?.data;
        if (s) {
          reset({
            receivableAccountId: s.defaultReceivableAccountId ?? "",
            revenueAccountId: s.defaultRevenueAccountId ?? "",
            taxPayableAccountId: s.defaultTaxPayableAccountId ?? "",
            cashAccountId: s.defaultCashAccountId ?? "",
          });
        }
      })
      .finally(() => setLoading(false));
  }, [reset]);

  const onSubmit = async (values: SalesSettingsFormValues) => {
    setSaving(true);
    setSaveError(null);
    setSaved(false);
    try {
      await configureSalesSettings(values);
      setSaved(true);
    } catch (err) {
      setSaveError(typeof err === "string" ? err : "Could not save Sales settings. Please try again.");
    } finally {
      setSaving(false);
    }
  };

  const activeAccounts = accounts.filter((a) => a.isActive);

  return (
    <>
      <PageBreadcrumb title="Sales Settings" name="Sales Settings" breadCrumbItems={["Sales", "Settings"]} />

      <div className="card max-w-2xl">
        <div className="card-body">
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-4">
            These GL accounts are used to automatically post journal entries when invoices are issued and payments are verified.
            All four must be configured before invoices can be issued.
          </p>

          {loading ? (
            <p className="text-slate-400 text-sm">Loading…</p>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Accounts Receivable</label>
                <select {...register("receivableAccountId", { required: true })} className="form-select w-full">
                  <option value="">Select account…</option>
                  {activeAccounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.code} {a.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Sales Revenue</label>
                <select {...register("revenueAccountId", { required: true })} className="form-select w-full">
                  <option value="">Select account…</option>
                  {activeAccounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.code} {a.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">SST Payable</label>
                <select {...register("taxPayableAccountId", { required: true })} className="form-select w-full">
                  <option value="">Select account…</option>
                  {activeAccounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.code} {a.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2">Default Cash / Bank</label>
                <select {...register("cashAccountId", { required: true })} className="form-select w-full">
                  <option value="">Select account…</option>
                  {activeAccounts.map((a) => (
                    <option key={a.id} value={a.id}>{a.code} {a.name}</option>
                  ))}
                </select>
              </div>

              {saveError && <div className="text-sm text-red-600">{saveError}</div>}
              {saved && <div className="text-sm text-emerald-600">Sales settings saved.</div>}

              <div className="flex justify-end pt-2">
                <button type="submit" disabled={saving} className="btn text-white bg-primary text-sm disabled:opacity-50">
                  {saving ? "Saving…" : "Save Settings"}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </>
  );
};

export default SalesSettingsPage;
