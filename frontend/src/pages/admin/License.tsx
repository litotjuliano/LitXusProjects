import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { Navigate } from "react-router-dom";

import { PageBreadcrumb } from "../../components";
import { APICore } from "../../helpers/api/apiCore";
import { getLicense, applyLicenseKey, type License as LicenseType } from "../../helpers/api/license";
import { authApiResponseSuccess } from "../../redux/auth/actions";
import { AuthActionTypes } from "../../redux/auth/constants";
import { AppDispatch, RootState } from "../../redux/store";

const api = new APICore();

const License = () => {
  const dispatch = useDispatch<AppDispatch>();
  const currentUser = useSelector((state: RootState) => state.Auth.user as any);

  // Menu hides this page from non-Super-Admins, but routes/index.tsx's `roles` field isn't
  // actually enforced by the router (see Routes.tsx) — this guard is what actually blocks
  // direct URL navigation to /admin/license for everyone else.
  const isSuperAdmin = (currentUser?.roles ?? []).includes("Super Admin");

  const [license, setLicense] = useState<LicenseType | null>(null);
  const [licenseLoading, setLicenseLoading] = useState(true);
  const [licenseForbidden, setLicenseForbidden] = useState(false);

  const [newKey, setNewKey] = useState("");
  const [applying, setApplying] = useState(false);
  const [applyError, setApplyError] = useState<string | null>(null);
  const [applyMessage, setApplyMessage] = useState<string | null>(null);

  const fetchLicense = () => {
    setLicenseLoading(true);
    setLicenseForbidden(false);
    getLicense()
      .then((res) => setLicense(res.data?.data ?? null))
      .catch((err) => {
        if (err === "Access Forbidden") setLicenseForbidden(true);
        setLicense(null);
      })
      .finally(() => setLicenseLoading(false));
  };

  useEffect(fetchLicense, []);

  const onApplyKey = async () => {
    setApplyError(null);
    setApplyMessage(null);

    if (!newKey.trim()) {
      setApplyError("Please paste a license key.");
      return;
    }

    setApplying(true);
    try {
      const res = await applyLicenseKey(newKey.trim());
      const updated: LicenseType = res.data?.data;
      setLicense(updated);
      setNewKey("");
      setApplyMessage("License key applied.");

      // Nav visibility reads enabledModules from the session (set at login) — refresh it here too,
      // otherwise the sidebar wouldn't reflect the change until the next login/token refresh.
      if (currentUser && updated) {
        const updatedSession = { ...currentUser, enabledModules: updated.enabledModules };
        api.setLoggedInUser(updatedSession);
        dispatch(authApiResponseSuccess(AuthActionTypes.LOGIN_USER, updatedSession));
      }
    } catch (err) {
      setApplyError(typeof err === "string" ? err : "Could not apply the license key. Please try again.");
    } finally {
      setApplying(false);
    }
  };

  if (!isSuperAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <>
      <PageBreadcrumb title="License" name="License" breadCrumbItems={["Administration", "License"]} />

      <div className="card mb-5">
        <div className="card-header">
          <h5 className="font-medium text-slate-900 dark:text-slate-200">Current License</h5>
        </div>
        <div className="card-body">
          {licenseLoading && <p className="text-sm text-slate-400">Loading…</p>}

          {!licenseLoading && licenseForbidden && (
            <p className="text-sm text-slate-500 dark:text-slate-400">
              You don't have permission to view license details.
            </p>
          )}

          {!licenseLoading && !licenseForbidden && license && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-slate-500 dark:text-slate-400">Product Code</p>
                <p className="font-medium text-slate-900 dark:text-slate-200">{license.productCode}</p>
              </div>
              <div>
                <p className="text-slate-500 dark:text-slate-400">Issued To</p>
                <p className="font-medium text-slate-900 dark:text-slate-200">{license.issuedToCompany}</p>
              </div>
              <div>
                <p className="text-slate-500 dark:text-slate-400">Issued On</p>
                <p className="font-medium text-slate-900 dark:text-slate-200">{new Date(license.issuedAtUtc).toLocaleDateString()}</p>
              </div>
              <div>
                <p className="text-slate-500 dark:text-slate-400">Expires On</p>
                <p className="font-medium text-slate-900 dark:text-slate-200">{new Date(license.expiresAtUtc).toLocaleDateString()}</p>
              </div>
              <div className="md:col-span-2">
                <p className="text-slate-500 dark:text-slate-400 mb-1">Enabled Modules</p>
                <div className="flex flex-wrap gap-1">
                  {license.enabledModules.length === 0 && <span className="text-slate-400">None</span>}
                  {license.enabledModules.map((m) => (
                    <span key={m} className="rounded bg-slate-100 dark:bg-slate-700 px-2 py-0.5 text-xs font-medium text-slate-700 dark:text-slate-200">
                      {m}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <h5 className="font-medium text-slate-900 dark:text-slate-200">Apply New License Key</h5>
        </div>
        <div className="card-body">
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-4">
            Paste a signed license key (generated offline via backend/tools/LitXus.LicenseGenerator). Product code,
            company, expiry, and enabled modules are all derived from the key's signature — they can't be edited
            independently. Applying a key takes effect immediately for all users.
          </p>
          <textarea
            value={newKey}
            onChange={(e) => setNewKey(e.target.value)}
            rows={4}
            placeholder="Paste the license key (JWT) here…"
            className="form-input w-full font-mono text-xs"
          />
          {applyError && <div className="text-sm text-red-600 mt-3">{applyError}</div>}
          {applyMessage && <div className="text-sm text-emerald-600 mt-3">{applyMessage}</div>}
          <div className="flex justify-end mt-4">
            <button
              type="button"
              disabled={applying}
              className="btn text-white bg-primary text-sm disabled:opacity-50"
              onClick={onApplyKey}
            >
              {applying ? "Applying…" : "Apply License Key"}
            </button>
          </div>
        </div>
      </div>
    </>
  );
};

export default License;
