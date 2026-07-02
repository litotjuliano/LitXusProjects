import { type CompanyProfile } from "../helpers/api/company";

interface ReportLetterheadProps {
  company: CompanyProfile | null;
}

const ReportLetterhead = ({ company }: ReportLetterheadProps) => {
  if (!company) return null;

  const addressLine1 = [company.addressLine1, company.addressLine2].filter(Boolean).join(", ");
  const addressLine2 = [company.postalCode, company.city].filter(Boolean).join(" ");
  const addressLine3 = [addressLine2, company.state, company.country].filter(Boolean).join(", ");
  const contactLine = [
    `SSM: ${company.ssmRegistrationNumber}`,
    `TIN: ${company.tin}`,
    company.phone,
  ].filter(Boolean).join("  •  ");

  return (
    <div className="text-center mb-5">
      <h5 className="font-semibold text-slate-900 dark:text-slate-100">{company.name}</h5>
      <p className="text-xs text-slate-500 dark:text-slate-400">{addressLine1}</p>
      <p className="text-xs text-slate-500 dark:text-slate-400">{addressLine3}</p>
      <p className="text-xs text-slate-400 dark:text-slate-500 mt-1">{contactLine}</p>
    </div>
  );
};

export default ReportLetterhead;
