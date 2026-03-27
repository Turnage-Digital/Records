export type UserStatus = "Invited" | "Active" | "Suspended";

export interface UserSummary {
  userId: string;
  email: string;
  displayName?: string;
  status: UserStatus;
}
