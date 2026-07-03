import { useEffect, useRef, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listBankAccounts,
  createBankAccount,
  listStatementLines,
  importStatementLines,
  listUnmatchedGLLines,
  getReconciliationStatus,
  matchStatementLine,
  unmatchStatementLine,
  type BankAccount,
  type BankStatementLine,
  type UnmatchedGLEntryLine,
  type ReconciliationStatus,
} from "../../helpers/api/bankReconciliation";
import { listAccounts, type Account } from "../../helpers/api/accounting";
import { formatCurrency } from "../../utils/currency";

interface NewBankAccountFormValues {
  accountId: string;
  bankName: string;
  accountNumber: string;
}

const fieldClass = "form-input w-full";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";

const BankReconciliation = () => {
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([]);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [bankAccountId, setBankAccountId] = useState("");

  const [statementLines, setStatementLines] = useState<BankStatementLine[]>([]);
  const [unmatchedGLLines, setUnmatchedGLLines] = useState<UnmatchedGLEntryLine[]>([]);
  const [status, setStatus] = useState<ReconciliationStatus | null>(null);
  const [loading, setLoading] = useState(false);

  const [selectedStatementLineId, setSelectedStatementLineId] = useState<string | null>(null);
  const [selectedGLLineId, setSelectedGLLineId] = useState<string | null>(null);
  const [matchError, setMatchError] = useState<string | null>(null);
  const [matching, setMatching] = useState(false);
  const [unmatchingId, setUnmatchingId] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const [importing, setImporting] = useState(false);

  const [showNewBankAccountModal, setShowNewBankAccountModal] = useState(false);
  const [newBankAccountError, setNewBankAccountError] = useState<string | null>(null);

  useEffect(() => {
    listBankAccounts()
      .then((res) => {
        const list: BankAccount[] = res.data?.data ?? [];
        setBankAccounts(list);
        if (list.length > 0) setBankAccountId((prev) => prev || list[0].id);
      })
      .catch(() => setBankAccounts([]));
    listAccounts()
      .then((res) => setAccounts(res.data?.data ?? []))
      .catch(() => setAccounts([]));
  }, []);

  const refreshReconciliationData = (id: string) => {
    setLoading(true);
    Promise.all([
      listStatementLines(id).then((res) => setStatementLines(res.data?.data ?? [])),
      listUnmatchedGLLines(id).then((res) => setUnmatchedGLLines(res.data?.data ?? [])),
      getReconciliationStatus(id).then((res) => setStatus(res.data?.data ?? null)),
    ]).finally(() => setLoading(false));
  };

  useEffect(() => {
    if (!bankAccountId) return;
    setSelectedStatementLineId(null);
    setSelectedGLLineId(null);
    refreshReconciliationData(bankAccountId);
  }, [bankAccountId]);

  const handleImport = async () => {
    const file = fileInputRef.current?.files?.[0];
    if (!file || !bankAccountId) return;
    setImportError(null);
    setImporting(true);
    try {
      await importStatementLines(bankAccountId, file);
      if (fileInputRef.current) fileInputRef.current.value = "";
      refreshReconciliationData(bankAccountId);
    } catch (err) {
      setImportError(typeof err === "string" ? err : "Could not import the CSV file. Please check the format and try again.");
    } finally {
      setImporting(false);
    }
  };

  const handleMatch = async () => {
    if (!selectedStatementLineId || !selectedGLLineId) return;
    setMatchError(null);
    setMatching(true);
    try {
      await matchStatementLine(selectedStatementLineId, selectedGLLineId);
      setSelectedStatementLineId(null);
      setSelectedGLLineId(null);
      refreshReconciliationData(bankAccountId);
    } catch (err) {
      setMatchError(typeof err === "string" ? err : "Could not match these lines. Please try again.");
    } finally {
      setMatching(false);
    }
  };

  const handleUnmatch = async (statementLineId: string) => {
    setUnmatchingId(statementLineId);
    try {
      await unmatchStatementLine(statementLineId);
      refreshReconciliationData(bankAccountId);
    } finally {
      setUnmatchingId(null);
    }
  };

  const newBankAccountResolver = yupResolver<NewBankAccountFormValues>(
    yup.object().shape({
      accountId: yup.string().required("Please select a GL account"),
      bankName: yup.string().required("Please enter a bank name"),
      accountNumber: yup.string().required("Please enter an account number"),
    })
  );

  const onCreateBankAccount = async (formData: NewBankAccountFormValues) => {
    setNewBankAccountError(null);
    try {
      const res = await createBankAccount(formData);
      setShowNewBankAccountModal(false);
      const created: BankAccount | undefined = res.data?.data;
      const list = await listBankAccounts();
      setBankAccounts(list.data?.data ?? []);
      if (created) setBankAccountId(created.id);
    } catch (err) {
      setNewBankAccountError(typeof err === "string" ? err : "Could not create the bank account. Please try again.");
    }
  };

  const unmatchedStatementLines = statementLines.filter((l) => !l.isReconciled);
  const matchedStatementLines = statementLines.filter((l) => l.isReconciled);

  return (
    <>
      <PageBreadcrumb title="Bank Reconciliation" name="Bank Reconciliation" breadCrumbItems={["Accounting", "Bank Reconciliation"]}>
        <div className="flex items-center gap-2">
          <select value={bankAccountId} onChange={(e) => setBankAccountId(e.target.value)} className="form-select text-sm">
            {bankAccounts.map((b) => (
              <option key={b.id} value={b.id}>{b.bankName} ****{b.accountNumber.slice(-4)}</option>
            ))}
          </select>
          <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => { setNewBankAccountError(null); setShowNewBankAccountModal(true); }}>
            + New Bank Account
          </button>
        </div>
      </PageBreadcrumb>

      {bankAccounts.length === 0 && !loading && (
        <div className="card"><div className="card-body text-sm text-slate-400">No bank accounts yet — add one to get started.</div></div>
      )}

      {bankAccountId && (
        <>
          <div className="card mb-4">
            <div className="card-body flex flex-wrap items-center gap-3">
              <input ref={fileInputRef} type="file" accept=".csv" className="form-input text-sm" />
              <button type="button" className="btn text-white bg-primary text-sm" onClick={handleImport} disabled={importing}>
                {importing ? "Importing…" : "Import Statement CSV"}
              </button>
              <span className="text-xs text-slate-400">Format: Date,Description,Amount (yyyy-MM-dd, signed amount)</span>
              {importError && <p className="text-sm text-red-600 w-full">{importError}</p>}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            <div className="card">
              <div className="card-body p-0">
                <h6 className="px-4 pt-4 pb-2 font-medium text-slate-900 dark:text-slate-200">Bank Statement Lines</h6>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                      <th className="px-4 py-2 font-medium">Date</th>
                      <th className="px-4 py-2 font-medium">Description</th>
                      <th className="px-4 py-2 font-medium text-right">Amount</th>
                      <th className="px-4 py-2 font-medium"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {loading && <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>}
                    {!loading && unmatchedStatementLines.length === 0 && matchedStatementLines.length === 0 && (
                      <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">No statement lines imported yet.</td></tr>
                    )}
                    {!loading && unmatchedStatementLines.map((l) => (
                      <tr
                        key={l.id}
                        onClick={() => setSelectedStatementLineId(l.id)}
                        className={`cursor-pointer border-b border-slate-100 dark:border-slate-800 ${selectedStatementLineId === l.id ? "bg-primary/10" : "hover:bg-slate-50 dark:hover:bg-slate-800/50"}`}
                      >
                        <td className="px-4 py-2">{l.transactionDate}</td>
                        <td className="px-4 py-2">{l.description}</td>
                        <td className="px-4 py-2 text-right">{formatCurrency(l.amount)}</td>
                        <td className="px-4 py-2"></td>
                      </tr>
                    ))}
                    {!loading && matchedStatementLines.map((l) => (
                      <tr key={l.id} className="border-b border-slate-100 dark:border-slate-800 text-slate-400">
                        <td className="px-4 py-2">{l.transactionDate}</td>
                        <td className="px-4 py-2">{l.description} <span className="text-emerald-600 text-xs">✓ matched</span></td>
                        <td className="px-4 py-2 text-right">{formatCurrency(l.amount)}</td>
                        <td className="px-4 py-2 text-right">
                          <button
                            type="button"
                            onClick={(e) => { e.stopPropagation(); handleUnmatch(l.id); }}
                            disabled={unmatchingId === l.id}
                            className="text-xs font-medium text-red-600 hover:underline disabled:opacity-50"
                          >
                            Unmatch
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="card">
              <div className="card-body p-0">
                <h6 className="px-4 pt-4 pb-2 font-medium text-slate-900 dark:text-slate-200">Unmatched GL Lines</h6>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-left text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700">
                      <th className="px-4 py-2 font-medium">Date</th>
                      <th className="px-4 py-2 font-medium">Description</th>
                      <th className="px-4 py-2 font-medium text-right">Dr</th>
                      <th className="px-4 py-2 font-medium text-right">Cr</th>
                    </tr>
                  </thead>
                  <tbody>
                    {loading && <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">Loading…</td></tr>}
                    {!loading && unmatchedGLLines.length === 0 && (
                      <tr><td colSpan={4} className="px-4 py-6 text-center text-slate-400">No unmatched Posted GL lines for this account.</td></tr>
                    )}
                    {!loading && unmatchedGLLines.map((l) => (
                      <tr
                        key={l.glEntryLineId}
                        onClick={() => setSelectedGLLineId(l.glEntryLineId)}
                        className={`cursor-pointer border-b border-slate-100 dark:border-slate-800 ${selectedGLLineId === l.glEntryLineId ? "bg-primary/10" : "hover:bg-slate-50 dark:hover:bg-slate-800/50"}`}
                      >
                        <td className="px-4 py-2">{l.entryDate}</td>
                        <td className="px-4 py-2">{l.entryNumber ?? "—"} {l.description}</td>
                        <td className="px-4 py-2 text-right">{l.debitAmount ? formatCurrency(l.debitAmount) : ""}</td>
                        <td className="px-4 py-2 text-right">{l.creditAmount ? formatCurrency(l.creditAmount) : ""}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between mt-4">
            <p className="text-sm font-medium text-slate-600 dark:text-slate-300">
              {status ? `${status.matchedStatementLines} of ${status.totalStatementLines} lines matched` : ""}
            </p>
            <div className="text-right">
              {matchError && <p className="text-sm text-red-600 mb-2">{matchError}</p>}
              <button
                type="button"
                className="btn text-white bg-primary text-sm"
                onClick={handleMatch}
                disabled={!selectedStatementLineId || !selectedGLLineId || matching}
              >
                {matching ? "Matching…" : "Match Selected"}
              </button>
            </div>
          </div>
        </>
      )}

      <ModalLayout showModal={showNewBankAccountModal} toggleModal={() => setShowNewBankAccountModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Bank Account</h5>
        <VerticalForm<NewBankAccountFormValues> onSubmit={onCreateBankAccount} resolver={newBankAccountResolver}>
          <FormInput
            label="GL Account"
            type="select"
            name="accountId"
            containerClass="mb-4"
            className="form-select"
            labelClassName={labelClass}
          >
            <option value="">Select an account…</option>
            {accounts.filter((a) => a.isActive).map((a) => (
              <option key={a.id} value={a.id}>{a.code} {a.name}</option>
            ))}
          </FormInput>
          <FormInput
            label="Bank Name"
            type="text"
            name="bankName"
            placeholder="Maybank"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Account Number"
            type="text"
            name="accountNumber"
            placeholder="1234567890"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <div className="text-sm text-red-600 mb-3">{newBankAccountError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowNewBankAccountModal(false)}>
              Cancel
            </button>
            <button type="submit" className="btn text-white bg-primary text-sm">
              Create Bank Account
            </button>
          </div>
        </VerticalForm>
      </ModalLayout>
    </>
  );
};

export default BankReconciliation;
