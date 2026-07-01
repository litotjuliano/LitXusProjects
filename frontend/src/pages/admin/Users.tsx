import { PageBreadcrumb } from "../../components";

const Users = () => {
  return (
    <>
      <PageBreadcrumb title="Users" name="Users" breadCrumbItems={["Administration", "Users"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          User management — wired to GET /admin/users once the backend endpoint is implemented.
        </div>
      </div>
    </>
  );
};

export default Users;
