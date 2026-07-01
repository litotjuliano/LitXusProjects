import { PageBreadcrumb } from "../../../components";

const GeneralLedger = () => {
  return (
    <>
      <PageBreadcrumb title="General Ledger" name="General Ledger" breadCrumbItems={["Accounting", "Reports", "General Ledger"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Wired to GET /accounting/reports/general-ledger once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default GeneralLedger;
