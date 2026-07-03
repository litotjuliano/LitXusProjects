import { APICore } from "./apiCore";

const api = new APICore();

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  lastLoginAtUtc: string | null;
}

export interface Role {
  id: string;
  name: string;
  description: string | null;
  permissions: string[];
}

export interface Permission {
  id: string;
  module: string;
  entity: string;
  operation: string;
  code: string;
}

export interface AuditLog {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  beforeValuesJson: string | null;
  afterValuesJson: string | null;
  reason: string | null;
  userId: string | null;
  userEmail: string | null;
  ipAddress: string | null;
  timestampUtc: string;
}

function listUsers() {
  return api.get("/admin/users", null);
}

function createUser(email: string, fullName: string, password: string, roleId: string) {
  return api.create("/admin/users", { email, fullName, password, roleId });
}

function updateUserStatus(id: string, isActive: boolean) {
  return api.updatePatch(`/admin/users/${id}/status`, { isActive });
}

function assignRole(userId: string, roleId: string) {
  return api.create(`/admin/users/${userId}/roles`, { roleId });
}

function revokeRole(userId: string, roleId: string) {
  return api.delete(`/admin/users/${userId}/roles/${roleId}`);
}

function listRoles() {
  return api.get("/admin/roles", null);
}

function listPermissions() {
  return api.get("/admin/permissions", null);
}

function listAuditLogs() {
  return api.get("/admin/audit-logs", null);
}

export { listUsers, createUser, updateUserStatus, assignRole, revokeRole, listRoles, listPermissions, listAuditLogs };
