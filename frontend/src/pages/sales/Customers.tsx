import { useEffect, useState } from "react";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

import { PageBreadcrumb, FormInput, VerticalForm, DataTable, type DataTableColumn } from "../../components";
import ModalLayout from "../../components/HeadlessUI/ModalLayout";
import {
  listCustomers,
  createCustomer,
  updateCustomer,
  setCustomerActive,
  type Customer,
} from "../../helpers/api/sales";
import { formatCurrency } from "../../utils/currency";

interface CustomerFormValues {
  code: string;
  companyName: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  creditLimit: number;
  paymentTermsDays: number;
}

interface EditCustomerFormValues {
  companyName: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  creditLimit: number;
  paymentTermsDays: number;
}

const fieldClass = "form-input w-full";
const labelClass = "block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2";

const Customers = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [showInactive, setShowInactive] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [editError, setEditError] = useState<string | null>(null);

  const fetchCustomers = () => {
    setLoading(true);
    listCustomers(showInactive)
      .then((res) => setCustomers(res.data?.data ?? []))
      .catch(() => setCustomers([]))
      .finally(() => setLoading(false));
  };

  useEffect(fetchCustomers, [showInactive]);

  const schemaResolver = yupResolver<CustomerFormValues>(
    yup.object().shape({
      code: yup.string().required("Please enter a customer code"),
      companyName: yup.string().required("Please enter a company name"),
      contactPerson: yup.string().default(""),
      email: yup.string().email("Must be a valid email").default(""),
      phone: yup.string().default(""),
      address: yup.string().default(""),
      creditLimit: yup.number().min(0).default(0),
      paymentTermsDays: yup.number().min(0).default(30),
    })
  );

  const editSchemaResolver = yupResolver<EditCustomerFormValues>(
    yup.object().shape({
      companyName: yup.string().required("Please enter a company name"),
      contactPerson: yup.string().default(""),
      email: yup.string().email("Must be a valid email").default(""),
      phone: yup.string().default(""),
      address: yup.string().default(""),
      creditLimit: yup.number().min(0).default(0),
      paymentTermsDays: yup.number().min(0).default(30),
    })
  );

  const onSubmit = async (formData: CustomerFormValues) => {
    setFormError(null);
    try {
      await createCustomer({
        ...formData,
        contactPerson: formData.contactPerson || null,
        email: formData.email || null,
        phone: formData.phone || null,
        address: formData.address || null,
      });
      setShowModal(false);
      fetchCustomers();
    } catch (err) {
      setFormError(typeof err === "string" ? err : "Could not create the customer. Please try again.");
    }
  };

  const onEditSubmit = async (formData: EditCustomerFormValues) => {
    if (!editingCustomer) return;
    setEditError(null);
    try {
      await updateCustomer(editingCustomer.id, {
        ...formData,
        contactPerson: formData.contactPerson || null,
        email: formData.email || null,
        phone: formData.phone || null,
        address: formData.address || null,
      });
      setEditingCustomer(null);
      fetchCustomers();
    } catch (err) {
      setEditError(typeof err === "string" ? err : "Could not update the customer. Please try again.");
    }
  };

  const toggleStatus = async (customer: Customer) => {
    setBusyId(customer.id);
    try {
      await setCustomerActive(customer.id, !customer.isActive);
      await fetchCustomers();
    } finally {
      setBusyId(null);
    }
  };

  const columns: DataTableColumn<Customer>[] = [
    { key: "code", header: "Code", render: (c) => c.code, sortAccessor: (c) => c.code },
    { key: "companyName", header: "Company", render: (c) => c.companyName, sortAccessor: (c) => c.companyName },
    { key: "contactPerson", header: "Contact", render: (c) => c.contactPerson ?? "—" },
    { key: "email", header: "Email", render: (c) => c.email ?? "—" },
    {
      key: "creditLimit", header: "Credit Limit", className: "text-right", headerClassName: "text-right",
      render: (c) => formatCurrency(c.creditLimit), sortAccessor: (c) => c.creditLimit,
    },
    { key: "paymentTermsDays", header: "Terms", render: (c) => `${c.paymentTermsDays} days` },
    {
      key: "status", header: "Status",
      render: (c) => <span className={c.isActive ? "text-emerald-600" : "text-slate-400"}>{c.isActive ? "Active" : "Inactive"}</span>,
      sortAccessor: (c) => (c.isActive ? 1 : 0),
    },
    {
      key: "actions", header: "", className: "text-right",
      render: (c) => (
        <div className="flex justify-end gap-3">
          <button onClick={() => { setEditError(null); setEditingCustomer(c); }} className="text-xs font-medium text-primary hover:underline">
            Edit
          </button>
          <button
            onClick={() => toggleStatus(c)}
            disabled={busyId === c.id}
            className="text-xs font-medium text-primary hover:underline disabled:opacity-50"
          >
            {c.isActive ? "Deactivate" : "Activate"}
          </button>
        </div>
      ),
    },
  ];

  return (
    <>
      <PageBreadcrumb title="Customers" name="Customers" breadCrumbItems={["Sales", "Customers"]}>
        <button className="btn text-white bg-primary text-sm" onClick={() => { setFormError(null); setShowModal(true); }}>
          + New Customer
        </button>
      </PageBreadcrumb>

      <label className="flex items-center gap-2 mb-3 text-sm text-slate-600 dark:text-slate-300">
        <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} className="form-checkbox rounded" />
        Show inactive customers
      </label>

      <DataTable<Customer>
        columns={columns}
        data={customers}
        rowKey={(c) => c.id}
        loading={loading}
        emptyMessage="No customers yet. Create your first customer to get started."
        searchPlaceholder="Search by code, company, or email…"
        searchAccessor={(c) => `${c.code} ${c.companyName} ${c.email ?? ""}`}
      />

      <ModalLayout showModal={showModal} toggleModal={() => setShowModal(false)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">New Customer</h5>
        <VerticalForm<CustomerFormValues> onSubmit={onSubmit} resolver={schemaResolver} defaultValues={{ creditLimit: 0, paymentTermsDays: 30 }}>
          <FormInput label="Code" type="text" name="code" placeholder="CUST-001" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Company Name" type="text" name="companyName" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Contact Person" type="text" name="contactPerson" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Email" type="text" name="email" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Phone" type="text" name="phone" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Address" type="text" name="address" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Credit Limit (RM)" type="number" name="creditLimit" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <FormInput label="Payment Terms (days)" type="number" name="paymentTermsDays" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
          <div className="text-sm text-red-600 mb-3">{formError}</div>
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setShowModal(false)}>Cancel</button>
            <button type="submit" className="btn text-white bg-primary text-sm">Create Customer</button>
          </div>
        </VerticalForm>
      </ModalLayout>

      <ModalLayout showModal={editingCustomer !== null} toggleModal={() => setEditingCustomer(null)} panelClassName="w-full max-w-lg bg-white dark:bg-slate-800 p-6">
        <h5 className="text-lg font-medium text-slate-900 dark:text-slate-200 mb-4">Edit Customer</h5>
        {editingCustomer && (
          <VerticalForm<EditCustomerFormValues>
            key={editingCustomer.id}
            onSubmit={onEditSubmit}
            resolver={editSchemaResolver}
            defaultValues={{
              companyName: editingCustomer.companyName,
              contactPerson: editingCustomer.contactPerson ?? "",
              email: editingCustomer.email ?? "",
              phone: editingCustomer.phone ?? "",
              address: editingCustomer.address ?? "",
              creditLimit: editingCustomer.creditLimit,
              paymentTermsDays: editingCustomer.paymentTermsDays,
            }}
          >
            <div className="mb-4 text-sm text-slate-500 dark:text-slate-400">
              Code <span className="font-mono">{editingCustomer.code}</span> can't be changed here.
            </div>
            <FormInput label="Company Name" type="text" name="companyName" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Contact Person" type="text" name="contactPerson" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Email" type="text" name="email" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Phone" type="text" name="phone" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Address" type="text" name="address" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Credit Limit (RM)" type="number" name="creditLimit" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <FormInput label="Payment Terms (days)" type="number" name="paymentTermsDays" containerClass="mb-4" className={fieldClass} labelClassName={labelClass} />
            <div className="text-sm text-red-600 mb-3">{editError}</div>
            <div className="flex justify-end gap-2 pt-2">
              <button type="button" className="btn bg-white border border-slate-300 text-sm" onClick={() => setEditingCustomer(null)}>Cancel</button>
              <button type="submit" className="btn text-white bg-primary text-sm">Save Changes</button>
            </div>
          </VerticalForm>
        )}
      </ModalLayout>
    </>
  );
};

export default Customers;
