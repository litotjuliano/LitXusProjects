import { useEffect } from "react";
import { Navigate, Link, useLocation } from "react-router-dom";
import { useForm } from "react-hook-form";

// form validation
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";

// redux
import { useDispatch, useSelector } from "react-redux";
import { AppDispatch, RootState } from "../../redux/store";
import { loginUser, resetAuth } from "../../redux/actions";

// components
import { FormInput, AuthLayout, PageBreadcrumb } from "../../components";

interface UserData {
  email: string;
  password: string;
}

// Dev-only convenience — never rendered in a production build (see the import.meta.env.DEV
// guard below). Shipping working credentials on a public login page would be a real exposure.
const DEMO_ACCOUNTS = [
  { role: "Super Admin", email: "superadmin@litxus.demo", password: "Demo@12345" },
  { role: "Admin", email: "admin@litxus.demo", password: "Demo@12345" },
];

const Login = () => {
  const dispatch = useDispatch<AppDispatch>();

  const { user, userLoggedIn, loading } = useSelector(
    (state: RootState) => ({
      user: state.Auth.user,
      loading: state.Auth.loading,
      error: state.Auth.error,
      userLoggedIn: state.Auth.userLoggedIn,
    })
  );

  useEffect(() => {
    dispatch(resetAuth());
  }, [dispatch]);

  /*
  form validation schema
  */
  const schemaResolver = yupResolver(
    yup.object().shape({
      email: yup.string().email("Please enter a valid email").required("Please enter your email"),
      password: yup.string().required("Please enter Password"),
    })
  );

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<UserData>({ resolver: schemaResolver });

  /*
  handle form submission
  */
  const onSubmit = (formData: UserData) => {
    dispatch(loginUser(formData["email"], formData["password"]));
  };

  const fillDemoAccount = (email: string, password: string) => {
    setValue("email", email, { shouldValidate: true });
    setValue("password", password, { shouldValidate: true });
  };

  const location = useLocation();

  // redirection back to where user got redirected from
  const redirectUrl = location?.search?.slice(6) || "/";

  return (
    <>
      {(userLoggedIn || user) && <Navigate to={redirectUrl} />}
      <PageBreadcrumb title="Login" />
      <AuthLayout
        authTitle="Sign In"
        helpText="Enter your email address and password to access admin panel."
      >
        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <FormInput
            label="Email Address"
            type="email"
            name="email"
            placeholder="Enter your email"
            containerClass="mb-4"
            className="form-input"
            labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2"
            register={register}
            errors={errors}
            required
          />

          <FormInput
            label="Password"
            type="password"
            name="password"
            placeholder="Enter your password"
            containerClass="mb-4"
            className="form-input"
            labelClassName="block text-sm font-medium text-gray-600 dark:text-gray-200 mb-2"
            register={register}
            errors={errors}
            required
          />

          <div className="flex items-center justify-between mb-4">
            <FormInput
              label="Remember me"
              type="checkbox"
              name="checkbox"
              containerClass="flex items-center"
              labelClassName="ms-2"
              className="form-checkbox rounded"
              register={register}
            />
            <Link to="/auth/recover-password" className="text-sm text-primary border-b border-dashed border-primary">Forget Password ?</Link>
          </div>

          <div className="flex justify-center mb-6">
            <button
              className="btn w-full text-white bg-primary"
              type="submit"
              disabled={loading}
            >
              Log In
            </button>
          </div>
        </form>

        {import.meta.env.DEV && (
          <div className="rounded-md border border-dashed border-slate-300 dark:border-slate-700 p-3">
            <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-400">
              Demo accounts (dev only — click to fill)
            </p>
            <div className="flex flex-col gap-1.5">
              {DEMO_ACCOUNTS.map((account) => (
                <button
                  key={account.email}
                  type="button"
                  onClick={() => fillDemoAccount(account.email, account.password)}
                  className="flex items-center justify-between rounded border border-slate-200 dark:border-slate-700 px-3 py-1.5 text-left text-xs hover:bg-slate-50 dark:hover:bg-slate-800"
                >
                  <span className="font-medium text-slate-700 dark:text-slate-200">{account.role}</span>
                  <span className="text-slate-400">{account.email}</span>
                </button>
              ))}
            </div>
          </div>
        )}
      </AuthLayout>
    </>
  )
}

export default Login
