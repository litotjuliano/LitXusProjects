import { APICore } from "./apiCore";

const api = new APICore();

export type TaxType = "Sst" | "IncomeTax";

export interface TaxCode {
  id: string;
  code: string;
  name: string;
  rate: number;
  type: TaxType;
}

function listTaxCodes() {
  return api.get("/accounting/tax-codes", null);
}

function createTaxCode(payload: Pick<TaxCode, "code" | "name" | "rate" | "type">) {
  return api.create("/accounting/tax-codes", payload);
}

function calculateSst(subTotal: number, taxCodeId: string) {
  return api.create("/accounting/tax/calculate-sst", { subTotal, taxCodeId });
}

export { listTaxCodes, createTaxCode, calculateSst };
