import * as React from "react";

export type AuthStatus = "checking" | "loggedOut" | "loggedIn";

export interface UserInfo {
  userName?: string;
  email?: string;
  name?: string;

  [key: string]: unknown;
}

interface AuthState {
  status: AuthStatus;
  username?: string;
  user: UserInfo | null;
}

export interface Auth {
  status: AuthStatus;
  username?: string;
  user: UserInfo | null;
  login: (username?: string) => void;
  logout: () => void;
  refresh: () => void;
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

const createLoggedOutState = (): AuthState => ({
  status: "loggedOut",
  username: undefined,
  user: null,
});

const createCheckingState = (username?: string): AuthState => ({
  status: "checking",
  username,
  user: null,
});

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [state, setState] = React.useState<AuthState>(() =>
    createCheckingState(),
  );
  const requestIdRef = React.useRef(0);

  const loadUserInfo = React.useCallback(async (usernameHint?: string) => {
    const requestId = ++requestIdRef.current;

    setState((prev) => createCheckingState(usernameHint ?? prev.username));

    try {
      const response = await fetch("/identity/manage/info", {
        credentials: "include",
      });

      if (requestId !== requestIdRef.current) {
        return;
      }

      if (response.status === 204 || response.status === 401) {
        setState(createLoggedOutState());
        return;
      }

      if (!response.ok) {
        throw new Error("Failed to load user info");
      }

      const info = (await response.json()) as UserInfo | null;
      const username = getUsernameFromInfo(info);
      setState({ status: "loggedIn", username, user: info ?? null });
    } catch (error) {
      if (requestId !== requestIdRef.current) {
        return;
      }
      setState(createLoggedOutState());
    }
  }, []);

  React.useEffect(() => {
    loadUserInfo().catch(() => {
      // handled inside loadUserInfo
    });

    return () => {
      requestIdRef.current += 1;
    };
  }, [loadUserInfo]);

  const login = React.useCallback(
    (username?: string) => {
      loadUserInfo(username).catch(() => {
        // handled inside loadUserInfo
      });
    },
    [loadUserInfo],
  );

  const logout = React.useCallback(() => {
    requestIdRef.current += 1;
    setState(createLoggedOutState());
  }, []);

  const refresh = React.useCallback(() => {
    loadUserInfo().catch(() => {
      // handled inside loadUserInfo
    });
  }, [loadUserInfo]);

  const value = React.useMemo<Auth>(
    () => ({
      status: state.status,
      username: state.username,
      user: state.user,
      login,
      logout,
      refresh,
    }),
    [state.status, state.username, state.user, login, logout, refresh],
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
