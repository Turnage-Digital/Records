export type UserRole = "GlobalAdmin" | "TenantAdmin" | "Operations";

export interface UserRoleMembership {
  userId: string;
  role: UserRole;
  tenantId?: string;
  grantedBy: string;
  grantedAt: string;
}
