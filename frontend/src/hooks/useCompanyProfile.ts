import { useEffect, useState } from "react";
import { getCompanyProfile, type CompanyProfile } from "../helpers/api/company";

/** Shared across the financial report pages for a lightweight company letterhead. */
const useCompanyProfile = () => {
  const [company, setCompany] = useState<CompanyProfile | null>(null);

  useEffect(() => {
    getCompanyProfile()
      .then((res) => setCompany(res.data?.data ?? null))
      .catch(() => setCompany(null));
  }, []);

  return company;
};

export default useCompanyProfile;
