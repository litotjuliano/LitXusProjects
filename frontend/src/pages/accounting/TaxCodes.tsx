import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import { listTaxCodes, createTaxCode, calculateSst, type TaxCode, type TaxType } from "../../helpers/api/taxCodes";
import { formatCurrency } from "../../utils/currency";

interface TaxCodeFormValues {
  code: string;
  name: string;
  rate: number;
  type: TaxType;
}

const TAX_TYPES: TaxType[] = ["Sst", "IncomeTax"];

const fieldClass = "form-input w-full";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";

const TaxCodes = () => {
  const [taxCodes, setTaxCodes] = useState<TaxCode[]>([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [calcTaxCodeId, setCalcTaxCodeId] = useState("");
  const [calcSubTotal, setCalcSubTotal] = useState("1000");
  const [calcResult, setCalcResult] = useState<{ sstAmount: number; total: number } | null>(null);
  const [calcError, setCalcError] = useState<string | null>(null);

  const fetchTaxCodes = () => {
    setLoading(true);
    listTaxCodes()
      .then((res) => {
        const list: TaxCode[] = res.data?.data ?? [];
        setTaxCodes(list);
        if (list.length > 0) setCalcTaxCodeId((prev) => prev || list[0].id);
      })
      .catch(() => setTaxCodes([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchTaxCodes, []);

  const schemaResolver = yupResolver<TaxCodeFormValues>(
    yup.object().shape({
      code: yup.string().required("Please enter a tax code"),
      name: yup.string().required("Please enter a name"),
      rate: yup.number().min(0, "Rate must be 0 or more").max(100, "Rate must be 100 or less").required("Please enter a rate"),
      type: yup.mixed<TaxType>().oneOf(TAX_TYPES).required("Please select a type"),
    })
  );

  const onSubmit = async (formData: TaxCodeFormValues) => {
    setFormError(null);
    try {
      await createTaxCode(formData);
      setShowModal(false);
      fetchTaxCodes();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not create the tax code. Please try again.");
    }
  };

  const handleCalculate = async () => {
    setCalcError(null);
    setCalcResult(null);
    const subTotal = Number(calcSubTotal);
    if (!calcTaxCodeId || Number.isNaN(subTotal)) return;
    try {
      const res = await calculateSst(subTotal, calcTaxCodeId);
      setCalcResult(res.data?.data ?? null);
    } catch (err) {
      setCalcError(typeof err === "string" ? err : "Could not calculate SST. Please try again.");
    }
  };

  const columns: DataTableColumn<TaxCode>[] = [
    { key: "code", header: "Code", render: (t) => t.code, sortAccessor: (t) => t.code },
    { key: "name", header: "Name", render: (t) => t.name },
    { key: "rate", header: "Rate", className: "text-right", headerClassName: "text-right", render: (t) => `${t.rate.toFixed(2)}%`, sortAccessor: (t) => t.rate },
    { key: "type", header: "Type", render: (t) => t.type, sortAccessor: (t) => t.type },
  ];

  return (
    <>
      <PageBreadcrumb title="Tax Codes" name="Tax Codes" breadCrumbItems={["Accounting", "Tax Codes"]}>
        <button
          className="btn text-white bg-primary text-sm"
          onClick={() => { setFormError(null); setShowModal(true); }}
        >
          + New Tax Code
        </button>
      </PageBreadcrumb>

      <DataTable<TaxCode>
        columns={columns}
        data={taxCodes}
        rowKey={(t) => t.id}
        loading={loading}
        emptyMessage="No tax codes yet. Create your first tax code to get started."
        searchPlaceholder="Search by code, name, or type…"
        searchAccessor={(t) => `${t.code} ${t.name} ${t.type}`}
      />

      <div className="card mt-5">
        <div className="card-body">
          <h5 className="mb-3 font-medium text-slate-900 dark:text-slate-200">Test Calculator</h5>
          <div className="flex flex-wrap items-end gap-3">
            <div>
              <label className={labelClass}>Tax Code</label>
              <select value={calcTaxCodeId} onChange={(e) => setCalcTaxCodeId(e.target.value)} className="form-select">
                {taxCodes.map((t) => (
                  <option key={t.id} value={t.id}>{t.code} — {t.name} ({t.rate}%)</option>
                ))}
              </select>
            </div>
            <div>
              <label className={labelClass}>Sub-Total (RM)</label>
              <input
                type="number"
                value={calcSubTotal}
                onChange={(e) => setCalcSubTotal(e.target.value)}
                className="form-input"
              />
            </div>
            <button type="button" className="btn text-white bg-primary text-sm" onClick={handleCalculate} disabled={!calcTaxCodeId}>
              Calculate
            </button>
          </div>
          {calcError && <p className="text-sm text-red-600 mt-3">{calcError}</p>}
          {calcResult && (
            <div className="mt-4 text-sm">
              <div className="flex justify-between max-w-xs">
                <span>Sub-Total</span>
                <span>{formatCurrency(Number(calcSubTotal))}</span>
              </div>
              <div className="flex justify-between max-w-xs">
                <span>SST Amount</span>
                <span>{formatCurrency(calcResult.sstAmount)}</span>
              </div>
              <div className="flex justify-between max-w-xs font-medium border-t border-slate-200 dark:border-slate-700 pt-1 mt-1">
                <span>Total</span>
                <span>{formatCurrency(calcResult.total)}</span>
              </div>
            </div>
          )}
        </div>
      </div>

      <ModalLayout showModal={showModal} toggleModal={() => setShowModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Tax Code</h5>
        <VerticalForm<TaxCodeFormValues> onSubmit={onSubmit} resolver={schemaResolver} defaultValues={{ type: "Sst" }}>
          <FormInput
            label="Code"
            type="text"
            name="code"
            placeholder="SST-6"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Name"
            type="text"
            name="name"
            placeholder="Sales & Service Tax 6%"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Rate (%)"
            type="number"
            name="rate"
            placeholder="6"
            containerClass="mb-4"
            className={fieldClass}
            labelClassName={labelClass}
          />
          <FormInput
            label="Type"
            type="select"
            name="type"
            containerClass="mb-4"
            className="form-select"
            labelClassName={labelClass}
          >
            {TAX_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </FormInput>
          <div className="text-sm text-red-600 mb-3">{formError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowModal(false)}>
              Cancel
            </button>
            <button type="submit" className="btn text-white bg-primary text-sm">
              Create Tax Code
            </button>
          </div>
        </VerticalForm>
      </ModalLayout>
    </>
  );
};

export default TaxCodes;
