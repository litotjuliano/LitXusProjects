import { Link } from 'react-router-dom'

// components
import { AuthLayout, PageBreadcrumb } from '../../components'

const BottomLink = () => {
  return (
    <p className="text-gray-500 dark:text-gray-400 text-center">
      Back to
      <Link to="/auth/login" className="text-primary ms-1">
        <b>Log In</b>
      </Link>
    </p>
  )
}

// There's no self-service password reset — no email infrastructure exists to deliver a reset
// link/token, and a self-service flow that hands a reset token to an anonymous caller would be
// an account-takeover risk with no way to send it safely. An Admin/Super Admin resets a user's
// password directly from Administration -> Users -> Reset Password instead (same trust model
// already used for account creation).
const RecoverPassword = () => {
  return (
    <>
      <PageBreadcrumb title='Recover Password' />

      <AuthLayout
        authTitle='Recover Password'
        helpText="There's no self-service password reset yet. Ask your Administrator to reset your password from Administration → Users — they'll set a new one and share it with you directly."
        bottomLinks={<BottomLink />}
      >
        <></>
      </AuthLayout>
    </>
  )
}

export default RecoverPassword
