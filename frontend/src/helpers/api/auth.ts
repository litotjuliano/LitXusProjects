import { APICore } from "./apiCore";

const api = new APICore();

// account
function login(params: { email: string; password: string }) {
  return api.create("/auth/login", params);
}

function logout() {
  return api.create("/auth/logout", {});
}

function signup(params: { fullName: string; email: string; password: string }) {
  return api.create("/auth/register", params);
}

function forgotPassword(params: { email: string }) {
  return api.create("/auth/forgot-password", params);
}

export { login, logout, signup, forgotPassword };
