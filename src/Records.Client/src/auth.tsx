import * as React from "react";

import { useQuery, useQueryClient } from "@tanstack/react-query";

import { sessionQueryKey, sessionQueryOptions } from "./query-options";

import type { AccessInfo, SessionInfo, UserInfo } from "./models/session";

export type AuthStatus = "checking" | "loggedOut" | "loggedIn";

export interface Auth {
  status: AuthStatus;
  username?: string;
  user: UserInfo | null;
  access: AccessInfo;
  login: () => Promise<SessionInfo | null>;
  logout: () => Promise<void>;
  refresh: () => Promise<SessionInfo | null>;
}

const AuthContext = React.createContext<Auth | undefined>(undefined);

const getUsernameFromInfo = (info: UserInfo | null | undefined) => {
  if (!info) {
    return undefined;
  }

  if (typeof info.userName === "string" && info.userName.length > 0) {
    return info.userName;
  }
  if (typeof info.email === "string" && info.email.length > 0) {
    return info.email;
  }
  if (typeof info.name === "string" && info.name.length > 0) {
    return info.name;
  }
  return undefined;
};

const defaultAccess: AccessInfo = {
  isGlobalAdmin: false,
  canAccessOps: false,
};

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const queryClient = useQueryClient();
  const sessionQuery = useQuery(sessionQueryOptions());
  const session = sessionQuery.data ?? null;

  const login = React.useCallback(async () => {
    await queryClient.cancelQueries();
    queryClient.clear();
    return queryClient.fetchQuery(sessionQueryOptions());
  }, [queryClient]);

  const logout = React.useCallback(async () => {
    await queryClient.cancelQueries();
    queryClient.clear();
    queryClient.setQueryData(sessionQueryKey, null);
  }, [queryClient]);

  const refresh = React.useCallback(() => {
    return queryClient.fetchQuery(sessionQueryOptions());
  }, [queryClient]);

  let status: AuthStatus = "loggedOut";
  if (sessionQuery.isPending) {
    status = "checking";
  } else if (session) {
    status = "loggedIn";
  }

  const username = getUsernameFromInfo(session?.user);

  const value = React.useMemo<Auth>(
    () => ({
      status,
      username,
      user: session?.user ?? null,
      access: session?.access ?? defaultAccess,
      login,
      logout,
      refresh,
    }),
    [status, username, session, login, logout, refresh],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): Auth => {
  const context = React.useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};
