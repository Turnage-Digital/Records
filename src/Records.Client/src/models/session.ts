export interface AccessInfo {
  isGlobalAdmin: boolean;
  canAccessOps: boolean;
}

export interface UserInfo {
  userName?: string;
  email?: string;
  name?: string;

  [key: string]: unknown;
}

export interface SessionInfo {
  user: UserInfo;
  access: AccessInfo;
}
