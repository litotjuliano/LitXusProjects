import { PageBreadcrumb } from "../../components";

const BankReconciliation = () => {
  return (
    <>
      <PageBreadcrumb title="Bank Reconciliation" name="Bank Reconciliation" breadCrumbItems={["Accounting", "Bank Reconciliation"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Bank statement import and matching — wired to GET /accounting/bank-accounts/{"{id}"}/statement-lines
          once the backend endpoints are implemented (see docs/phase-1-accounting/API_Specification.md).
        </div>
      </div>
    </>
  );
};

export default BankReconciliation;
