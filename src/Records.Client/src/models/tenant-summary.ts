export type TenantStatus = "Active" | "Disabled";

export interface TenantSummary {
  tenantId: string;
  name: string;
  status: TenantStatus;
  createdAt: string;
}
