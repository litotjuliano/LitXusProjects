import { PageBreadcrumb } from "../../../components";

const BalanceSheet = () => {
  return (
    <>
      <PageBreadcrumb title="Balance Sheet" name="Balance Sheet" breadCrumbItems={["Accounting", "Reports", "Balance Sheet"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Wired to GET /accounting/reports/balance-sheet once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default BalanceSheet;
