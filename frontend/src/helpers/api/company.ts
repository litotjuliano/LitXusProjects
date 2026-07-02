import { APICore } from "./apiCore";

const api = new APICore();

export type BusinessType = "PrivateCompany" | "PublicCompany" | "SoleProprietor" | "Partnership" | "Other";
export type AccountingFramework = "Mpers" | "Mfrs";

export interface CompanyProfile {
  id: string;
  name: string;
  ssmRegistrationNumber: string;
  tin: string;
  usid: string | null;
  businessRegistrationNumber: string | null;
  businessType: BusinessType;
  msicCode: string;
  principalBusinessActivity: string;
  establishmentDate: string | null;
  financialYearEndMonth: number;
  financialYearEndDay: number;
  accountingFramework: AccountingFramework;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone: string;
  secondaryPhone: string | null;
  email: string;
  website: string | null;
  primaryBankName: string | null;
  primaryBankAccountNumber: string | null;
  primaryBankAccountHolderName: string | null;
  primaryBankSwiftCode: string | null;
  sstRegistrationNumber: string | null;
  eisNumber: string | null;
  epfNumber: string | null;
  socsoNumber: string | null;
  externalAuditorName: string | null;
  companySecretaryName: string | null;
}

export interface CompanySignatory {
  id: string;
  companyId: string;
  name: string;
  icNumber: string;
  position: string;
  email: string;
  phone: string | null;
}

function getCompanyProfile() {
  return api.get("/company/profile", null);
}

function updateCompanyProfile(payload: Omit<CompanyProfile, "id">) {
  return api.update("/company/profile", payload);
}

function listSignatories() {
  return api.get("/company/signatories", null);
}

function addSignatory(payload: Omit<CompanySignatory, "id" | "companyId">) {
  return api.create("/company/signatories", payload);
}

function removeSignatory(id: string) {
  return api.delete(`/company/signatories/${id}`);
}

export { getCompanyProfile, updateCompanyProfile, listSignatories, addSignatory, removeSignatory };
