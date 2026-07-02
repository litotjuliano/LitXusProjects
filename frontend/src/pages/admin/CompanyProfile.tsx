import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  getCompanyProfile,
  updateCompanyProfile,
  listSignatories,
  addSignatory,
  removeSignatory,
  type CompanyProfile as CompanyProfileType,
  type CompanySignatory,
  type BusinessType,
  type AccountingFramework,
} from "../../helpers/api/company";

interface CompanyProfileFormValues {
  name: string;
  ssmRegistrationNumber: string;
  tin: string;
  usid: string;
  businessRegistrationNumber: string;
  businessType: BusinessType;
  msicCode: string;
  principalBusinessActivity: string;
  establishmentDate: string;
  financialYearEndMonth: string;
  financialYearEndDay: string;
  accountingFramework: AccountingFramework;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone: string;
  secondaryPhone: string;
  email: string;
  website: string;
  primaryBankName: string;
  primaryBankAccountNumber: string;
  primaryBankAccountHolderName: string;
  primaryBankSwiftCode: string;
  sstRegistrationNumber: string;
  eisNumber: string;
  epfNumber: string;
  socsoNumber: string;
  externalAuditorName: string;
  companySecretaryName: string;
}

interface SignatoryFormValues {
  name: string;
  icNumber: string;
  position: string;
  email: string;
  phone: string;
}

const BUSINESS_TYPES: BusinessType[] = ["PrivateCompany", "PublicCompany", "SoleProprietor", "Partnership", "Other"];
const ACCOUNTING_FRAMEWORKS: AccountingFramework[] = ["Mpers", "Mfrs"];

const sectionHeaderClass = "col-span-full font-medium text-slate-900 dark:text-slate-200 mt-4 first:mt-0 pb-1 border-b border-slate-200 dark:border-slate-700";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";
const fieldClass = "form-input w-full";

const emptyDefaults: CompanyProfileFormValues = {
  name: "", ssmRegistrationNumber: "", tin: "", usid: "", businessRegistrationNumber: "",
  businessType: "PrivateCompany", msicCode: "", principalBusinessActivity: "", establishmentDate: "",
  financialYearEndMonth: "12", financialYearEndDay: "31", accountingFramework: "Mpers",
  addressLine1: "", addressLine2: "", city: "", state: "", postalCode: "", country: "Malaysia",
  phone: "", secondaryPhone: "", email: "", website: "",
  primaryBankName: "", primaryBankAccountNumber: "", primaryBankAccountHolderName: "", primaryBankSwiftCode: "",
  sstRegistrationNumber: "", eisNumber: "", epfNumber: "", socsoNumber: "",
  externalAuditorName: "", companySecretaryName: "",
};

const toFormValues = (p: CompanyProfileType): CompanyProfileFormValues => ({
  name: p.name, ssmRegistrationNumber: p.ssmRegistrationNumber, tin: p.tin,
  usid: p.usid ?? "", businessRegistrationNumber: p.businessRegistrationNumber ?? "",
  businessType: p.businessType, msicCode: p.msicCode, principalBusinessActivity: p.principalBusinessActivity,
  establishmentDate: p.establishmentDate ?? "",
  financialYearEndMonth: String(p.financialYearEndMonth), financialYearEndDay: String(p.financialYearEndDay),
  accountingFramework: p.accountingFramework,
  addressLine1: p.addressLine1, addressLine2: p.addressLine2 ?? "", city: p.city, state: p.state,
  postalCode: p.postalCode, country: p.country,
  phone: p.phone, secondaryPhone: p.secondaryPhone ?? "", email: p.email, website: p.website ?? "",
  primaryBankName: p.primaryBankName ?? "", primaryBankAccountNumber: p.primaryBankAccountNumber ?? "",
  primaryBankAccountHolderName: p.primaryBankAccountHolderName ?? "", primaryBankSwiftCode: p.primaryBankSwiftCode ?? "",
  sstRegistrationNumber: p.sstRegistrationNumber ?? "", eisNumber: p.eisNumber ?? "",
  epfNumber: p.epfNumber ?? "", socsoNumber: p.socsoNumber ?? "",
  externalAuditorName: p.externalAuditorName ?? "", companySecretaryName: p.companySecretaryName ?? "",
});

const blankToNull = (v: string) => (v.trim() === "" ? null : v);

const CompanyProfile = () => {
  const [defaultValues, setDefaultValues] = useState<CompanyProfileFormValues>(emptyDefaults);
  const [loading, setLoading] = useState(true);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  const [signatories, setSignatories] = useState<CompanySignatory[]>([]);
  const [signatoriesLoading, setSignatoriesLoading] = useState(true);
  const [showSignatoryModal, setShowSignatoryModal] = useState(false);
  const [signatoryError, setSignatoryError] = useState<string | null>(null);

  const fetchProfile = () => {
    setLoading(true);
    getCompanyProfile()
      .then((res) => {
        const profile = res.data?.data as CompanyProfileType | null;
        if (profile) setDefaultValues(toFormValues(profile));
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  };

  const fetchSignatories = () => {
    setSignatoriesLoading(true);
    listSignatories()
      .then((res) => setSignatories(res.data?.data ?? []))
      .catch(() => setSignatories([]))
      .finally(() => setSignatoriesLoading(false));
  };

  useEffect(() => {
    fetchProfile();
    fetchSignatories();
  }, []);

  const schemaResolver = yupResolver<CompanyProfileFormValues>(
    yup.object().shape({
      name: yup.string().required("Company name is required"),
      ssmRegistrationNumber: yup.string().required("SSM registration number is required"),
      tin: yup.string().required("Tax Identification Number is required"),
      usid: yup.string().default(""),
      businessRegistrationNumber: yup.string().default(""),
      businessType: yup.mixed<BusinessType>().oneOf(BUSINESS_TYPES).required(),
      msicCode: yup.string().required("MSIC code is required"),
      principalBusinessActivity: yup.string().required("Principal business activity is required"),
      establishmentDate: yup.string().default(""),
      financialYearEndMonth: yup.string().required().test("range", "Must be 1-12", (v) => {
        const n = Number(v); return n >= 1 && n <= 12;
      }),
      financialYearEndDay: yup.string().required().test("range", "Must be 1-31", (v) => {
        const n = Number(v); return n >= 1 && n <= 31;
      }),
      accountingFramework: yup.mixed<AccountingFramework>().oneOf(ACCOUNTING_FRAMEWORKS).required(),
      addressLine1: yup.string().required("Address is required"),
      addressLine2: yup.string().default(""),
      city: yup.string().required("City is required"),
      state: yup.string().required("State is required"),
      postalCode: yup.string().required("Postal code is required"),
      country: yup.string().required("Country is required"),
      phone: yup.string().required("Phone is required"),
      secondaryPhone: yup.string().default(""),
      email: yup.string().email("Must be a valid email").required("Email is required"),
      website: yup.string().default(""),
      primaryBankName: yup.string().default(""),
      primaryBankAccountNumber: yup.string().default(""),
      primaryBankAccountHolderName: yup.string().default(""),
      primaryBankSwiftCode: yup.string().default(""),
      sstRegistrationNumber: yup.string().default(""),
      eisNumber: yup.string().default(""),
      epfNumber: yup.string().default(""),
      socsoNumber: yup.string().default(""),
      externalAuditorName: yup.string().default(""),
      companySecretaryName: yup.string().default(""),
    })
  );

  const onSubmit = async (values: CompanyProfileFormValues) => {
    setSaveError(null);
    setSaveMessage(null);
    try {
      await updateCompanyProfile({
        name: values.name,
        ssmRegistrationNumber: values.ssmRegistrationNumber,
        tin: values.tin,
        usid: blankToNull(values.usid),
        businessRegistrationNumber: blankToNull(values.businessRegistrationNumber),
        businessType: values.businessType,
        msicCode: values.msicCode,
        principalBusinessActivity: values.principalBusinessActivity,
        establishmentDate: blankToNull(values.establishmentDate),
        financialYearEndMonth: Number(values.financialYearEndMonth),
        financialYearEndDay: Number(values.financialYearEndDay),
        accountingFramework: values.accountingFramework,
        addressLine1: values.addressLine1,
        addressLine2: blankToNull(values.addressLine2),
        city: values.city,
        state: values.state,
        postalCode: values.postalCode,
        country: values.country,
        phone: values.phone,
        secondaryPhone: blankToNull(values.secondaryPhone),
        email: values.email,
        website: blankToNull(values.website),
        primaryBankName: blankToNull(values.primaryBankName),
        primaryBankAccountNumber: blankToNull(values.primaryBankAccountNumber),
        primaryBankAccountHolderName: blankToNull(values.primaryBankAccountHolderName),
        primaryBankSwiftCode: blankToNull(values.primaryBankSwiftCode),
        sstRegistrationNumber: blankToNull(values.sstRegistrationNumber),
        eisNumber: blankToNull(values.eisNumber),
        epfNumber: blankToNull(values.epfNumber),
        socsoNumber: blankToNull(values.socsoNumber),
        externalAuditorName: blankToNull(values.externalAuditorName),
        companySecretaryName: blankToNull(values.companySecretaryName),
      } as unknown as Omit<CompanyProfileType, "id">);
      setSaveMessage("Company profile saved.");
      fetchProfile();
    } catch (err) {
      setSaveError(typeof err === "string" ? err : "Could not save the company profile. Please try again.");
    }
  };

  const signatoryColumns: DataTableColumn<CompanySignatory>[] = [
    { key: "name", header: "Name", render: (s) => s.name, sortAccessor: (s) => s.name },
    { key: "icNumber", header: "IC Number", render: (s) => s.icNumber },
    { key: "position", header: "Position", render: (s) => s.position, sortAccessor: (s) => s.position },
    { key: "email", header: "Email", render: (s) => s.email },
    { key: "phone", header: "Phone", render: (s) => s.phone ?? "—" },
    {
      key: "actions",
      header: "",
      className: "text-right",
      render: (s) => (
        <button
          onClick={() => handleRemoveSignatory(s.id)}
          className="text-xs font-medium text-red-600 hover:underline"
        >
          Remove
        </button>
      ),
    },
  ];

  const handleRemoveSignatory = async (id: string) => {
    await removeSignatory(id);
    fetchSignatories();
  };

  const signatorySchemaResolver = yupResolver<SignatoryFormValues>(
    yup.object().shape({
      name: yup.string().required("Please enter a name"),
      icNumber: yup.string().required("Please enter an IC number"),
      position: yup.string().required("Please enter a position"),
      email: yup.string().email("Must be a valid email").required("Please enter an email"),
      phone: yup.string().default(""),
    })
  );

  const onAddSignatory = async (values: SignatoryFormValues) => {
    setSignatoryError(null);
    try {
      await addSignatory({ ...values, phone: values.phone || null } as unknown as Omit<CompanySignatory, "id" | "companyId">);
      setShowSignatoryModal(false);
      fetchSignatories();
    } catch (err) {
      setSignatoryError(typeof err === "string" ? err : "Could not add the signatory. Please try again.");
    }
  };

  if (loading) {
    return (
      <>
        <PageBreadcrumb title="Company Profile" name="Company Profile" breadCrumbItems={["Administration", "Company Profile"]} />
        <div className="card"><div className="card-body text-sm text-slate-400">Loading…</div></div>
      </>
    );
  }

  return (
    <>
      <PageBreadcrumb title="Company Profile" name="Company Profile" breadCrumbItems={["Administration", "Company Profile"]} />

      <div className="card mb-5">
        <div className="card-body">
          <VerticalForm<CompanyProfileFormValues>
            onSubmit={onSubmit}
            resolver={schemaResolver}
            defaultValues={defaultValues}
            formClass="grid grid-cols-1 md:grid-cols-2 gap-4"
          >
            <h6 className={`${sectionHeaderClass} mt-0`}>Registration</h6>
            <FormInput label="Company Name" type="text" name="name" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="SSM Registration Number" type="text" name="ssmRegistrationNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Tax Identification Number (TIN)" type="text" name="tin" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="USID (assigned by LHDN, if registered)" type="text" name="usid" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Business Registration Number" type="text" name="businessRegistrationNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />

            <h6 className={sectionHeaderClass}>Business Details</h6>
            <FormInput label="Business Type" type="select" name="businessType" containerClass="mb-2" className="form-select" labelClassName={labelClass}>
              {BUSINESS_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </FormInput>
            <FormInput label="Accounting Framework" type="select" name="accountingFramework" containerClass="mb-2" className="form-select" labelClassName={labelClass}>
              {ACCOUNTING_FRAMEWORKS.map((f) => <option key={f} value={f}>{f.toUpperCase()}</option>)}
            </FormInput>
            <FormInput label="MSIC Code" type="text" name="msicCode" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Establishment Date" type="date" name="establishmentDate" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Financial Year-End Month (1-12)" type="number" min={1} max={12} name="financialYearEndMonth" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Financial Year-End Day" type="number" min={1} max={31} name="financialYearEndDay" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Principal Business Activity" type="text" name="principalBusinessActivity" containerClass="mb-2 md:col-span-2" className={fieldClass} labelClassName={labelClass} />

            <h6 className={sectionHeaderClass}>Address &amp; Contact</h6>
            <FormInput label="Address Line 1" type="text" name="addressLine1" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Address Line 2" type="text" name="addressLine2" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="City" type="text" name="city" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="State" type="text" name="state" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Postal Code" type="text" name="postalCode" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Country" type="text" name="country" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Phone" type="text" name="phone" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Secondary Phone" type="text" name="secondaryPhone" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Email" type="text" name="email" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Website" type="text" name="website" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />

            <h6 className={sectionHeaderClass}>Primary Bank (optional)</h6>
            <FormInput label="Bank Name" type="text" name="primaryBankName" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Account Number" type="text" name="primaryBankAccountNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Account Holder Name" type="text" name="primaryBankAccountHolderName" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="SWIFT Code" type="text" name="primaryBankSwiftCode" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />

            <h6 className={sectionHeaderClass}>Statutory Numbers (optional)</h6>
            <FormInput label="SST Registration Number" type="text" name="sstRegistrationNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="EIS Number" type="text" name="eisNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="EPF Number" type="text" name="epfNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="SOCSO Number" type="text" name="socsoNumber" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />

            <h6 className={sectionHeaderClass}>Governance (optional)</h6>
            <FormInput label="External Auditor" type="text" name="externalAuditorName" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Company Secretary" type="text" name="companySecretaryName" containerClass="mb-2" className={fieldClass} labelClassName={labelClass} />

            <div className="col-span-full text-sm text-red-600">{saveError}</div>
            <div className="col-span-full text-sm text-emerald-600">{saveMessage}</div>

            <div className="col-span-full flex justify-end pt-2">
              <button type="submit" className="btn text-white bg-primary text-sm">Save Company Profile</button>
            </div>
          </VerticalForm>
        </div>
      </div>

      <div className="flex items-center justify-between mb-3">
        <h5 className="font-medium text-slate-900 dark:text-slate-200">Authorized Signatories</h5>
        <button
          className="btn text-white bg-primary text-sm"
          onClick={() => { setSignatoryError(null); setShowSignatoryModal(true); }}
        >
          + Add Signatory
        </button>
      </div>
      <DataTable<CompanySignatory>
        columns={signatoryColumns}
        data={signatories}
        rowKey={(s) => s.id}
        loading={signatoriesLoading}
        emptyMessage="No signatories added yet."
      />

      <ModalLayout showModal={showSignatoryModal} toggleModal={() => setShowSignatoryModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">Add Signatory</h5>
        <VerticalForm<SignatoryFormValues> onSubmit={onAddSignatory} resolver={signatorySchemaResolver}>
          <FormInput label="Name" type="text" name="name" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="IC Number" type="text" name="icNumber" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Position" type="text" name="position" placeholder="Director / Finance Manager / Backup Person" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Email" type="text" name="email" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Phone" type="text" name="phone" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <div className="text-sm text-red-600 mb-3">{signatoryError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowSignatoryModal(false)}>
              Cancel
            </button>
            <button type="submit" className="btn text-white bg-primary text-sm">
              Add Signatory
            </button>
          </div>
        </VerticalForm>
      </ModalLayout>
    </>
  );
};

export default CompanyProfile;
