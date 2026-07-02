import { APICore } from "./apiCore";

const api = new APICore();

export interface License {
  id: string;
  productCode: string;
  issuedToCompany: string;
  issuedAtUtc: string;
  expiresAtUtc: string;
  licenseKey: string;
  enabledModules: string[];
}

function getLicense() {
  return api.get("/admin/license", null);
}

function applyLicenseKey(licenseKey: string) {
  return api.create("/admin/license/apply-key", { licenseKey });
}

export { getLicense, applyLicenseKey };
