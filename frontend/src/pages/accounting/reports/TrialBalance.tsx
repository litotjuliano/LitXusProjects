import { PageBreadcrumb } from "../../../components";

const TrialBalance = () => {
  return (
    <>
      <PageBreadcrumb title="Trial Balance" name="Trial Balance" breadCrumbItems={["Accounting", "Reports", "Trial Balance"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Wired to GET /accounting/reports/trial-balance once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default TrialBalance;
