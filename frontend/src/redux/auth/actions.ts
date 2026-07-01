// constants
import { AuthActionTypes } from "./constants";

export interface AuthActionType {
  type:
  | AuthActionTypes.API_RESPONSE_SUCCESS
  | AuthActionTypes.API_RESPONSE_ERROR
  | AuthActionTypes.FORGOT_PASSWORD
  | AuthActionTypes.FORGOT_PASSWORD_CHANGE
  | AuthActionTypes.LOGIN_USER
  | AuthActionTypes.LOGOUT_USER
  | AuthActionTypes.RESET
  | AuthActionTypes.SIGNUP_USER;
  payload: {} | string;
}

interface UserSession {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
  permissions: string[];
  enabledModules: string[];
  accessToken: string;
  refreshToken: string;
}

// common success
export const authApiResponseSuccess = (
  actionType: string,
  data: UserSession | {}
): AuthActionType => ({
  type: AuthActionTypes.API_RESPONSE_SUCCESS,
  payload: { actionType, data },
});
// common error
export const authApiResponseError = (
  actionType: string,
  error: string
): AuthActionType => ({
  type: AuthActionTypes.API_RESPONSE_ERROR,
  payload: { actionType, error },
});

export const loginUser = (email: string, password: string): AuthActionType => ({
  type: AuthActionTypes.LOGIN_USER,
  payload: { email, password },
});

export const logoutUser = (): AuthActionType => ({
  type: AuthActionTypes.LOGOUT_USER,
  payload: {},
});

export const signupUser = (
  fullName: string,
  email: string,
  password: string
): AuthActionType => ({
  type: AuthActionTypes.SIGNUP_USER,
  payload: { fullName, email, password },
});

export const forgotPassword = (email: string): AuthActionType => ({
  type: AuthActionTypes.FORGOT_PASSWORD,
  payload: { email },
});

export const resetAuth = (): AuthActionType => ({
  type: AuthActionTypes.RESET,
  payload: {},
});
