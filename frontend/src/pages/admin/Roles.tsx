import { PageBreadcrumb } from "../../components";

const Roles = () => {
  return (
    <>
      <PageBreadcrumb title="Roles & Permissions" name="Roles & Permissions" breadCrumbItems={["Administration", "Roles"]} />
      <div className="card">
        <div className="card-body text-sm text-slate-500 dark:text-slate-400">
          Role/permission matrix — wired to GET /admin/roles and GET /admin/permissions once implemented.
        </div>
      </div>
    </>
  );
};

export default Roles;
