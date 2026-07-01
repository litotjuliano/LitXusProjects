import { PageBreadcrumb } from "../../../components";

const IncomeStatement = () => {
  return (
    <>
      <PageBreadcrumb title="Income Statement" name="Income Statement" breadCrumbItems={["Accounting", "Reports", "Income Statement"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Wired to GET /accounting/reports/income-statement once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default IncomeStatement;
