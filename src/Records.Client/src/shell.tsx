import * as React from "react";

import MenuIcon from "@mui/icons-material/Menu";
import {
  AppBar,
  Box,
  CircularProgress,
  Container,
  IconButton,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { Navigate, Outlet, useLocation } from "react-router-dom";

import { useAuth } from "./auth";
import AppSidebar from "./components/app-sidebar/app-sidebar";
import NotificationsBell from "./components/notifications/notifications-bell";
import UserMenu from "./components/user-menu";
import { connectChangeFeed, createChangeFeedRouter } from "./lib/sse";

const collapsedSidebarWidth = 88;
const expandedSidebarWidth = 320;

const getSectionLabel = (pathname: string) => {
  if (pathname === "/" || pathname.startsWith("/threads/")) {
    return "Agents";
  }

  if (pathname.startsWith("/recordsets")) {
    return "Recordsets";
  }

  if (pathname.startsWith("/admin/tenants")) {
    return "Tenants";
  }

  if (pathname.startsWith("/admin/users")) {
    return "Users";
  }

  return "Records";
};

const getPageLabel = (pathname: string) => {
  if (pathname === "/") {
    return "Chat-first operations";
  }

  if (pathname.startsWith("/threads/")) {
    return "Conversation";
  }

  if (pathname === "/recordsets") {
    return "Browse recordsets";
  }

  if (pathname === "/recordsets/create") {
    return "Create a recordset";
  }

  if (/^\/recordsets\/[^/]+\/edit$/.test(pathname)) {
    return "Edit recordset";
  }

  if (/^\/recordsets\/[^/]+\/records\/create$/.test(pathname)) {
    return "Create record";
  }

  if (/^\/recordsets\/[^/]+\/records\/[^/]+\/edit$/.test(pathname)) {
    return "Edit record";
  }

  if (/^\/recordsets\/[^/]+\/records\/[^/]+$/.test(pathname)) {
    return "Record details";
  }

  if (/^\/recordsets\/[^/]+\/records$/.test(pathname)) {
    return "Record list";
  }

  if (pathname.startsWith("/admin/tenants")) {
    return "Tenant administration";
  }

  if (pathname.startsWith("/admin/users")) {
    return "User administration";
  }

  return "Operational tracking";
};

const Shell = () => {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const location = useLocation();
  const [sidebarCollapsed, setSidebarCollapsed] = React.useState(false);
  const [mobileSidebarOpen, setMobileSidebarOpen] = React.useState(false);

  React.useEffect(() => {
    if (auth.status !== "loggedIn") {
      return undefined;
    }

    const handler = createChangeFeedRouter({
      "Records.Recordsets.Contracts.IntegrationEvents.RecordCreatedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({
            queryKey: ["recordset-records"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["record-history"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-history"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-names"],
            exact: false,
          });
        },
      "Records.Recordsets.Contracts.IntegrationEvents.RecordUpdatedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({
            queryKey: ["recordset-records"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["record"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["record-history"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-history"],
            exact: false,
          });
        },
      "Records.Recordsets.Contracts.IntegrationEvents.RecordDeletedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({
            queryKey: ["recordset-records"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["record"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["record-history"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-names"],
            exact: false,
          });
        },
      "Records.Recordsets.Contracts.IntegrationEvents.RecordsetCreatedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({ queryKey: ["recordset-names"] });
          queryClient.invalidateQueries({
            queryKey: ["recordset-definition"],
            exact: false,
          });
        },
      "Records.Recordsets.Contracts.IntegrationEvents.RecordsetUpdatedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({ queryKey: ["recordset-names"] });
          queryClient.invalidateQueries({
            queryKey: ["recordset-definition"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-history"],
            exact: false,
          });
        },
      "Records.Recordsets.Contracts.IntegrationEvents.RecordsetDeletedIntegrationEvent":
        () => {
          queryClient.invalidateQueries({ queryKey: ["recordset-names"] });
          queryClient.invalidateQueries({
            queryKey: ["recordset-records"],
            exact: false,
          });
          queryClient.invalidateQueries({
            queryKey: ["recordset-history"],
            exact: false,
          });
        },
      "Records.Clocks.Domain.Events.ClockDefinitionCreated": () => {
        queryClient.invalidateQueries({
          queryKey: ["clock-definitions"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockDefinitionUpdated": () => {
        queryClient.invalidateQueries({
          queryKey: ["clock-definitions"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockDefinitionDisabled": () => {
        queryClient.invalidateQueries({
          queryKey: ["clock-definitions"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockStarted": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockPaused": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockResumed": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockAtRisk": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockBreached": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Clocks.Domain.Events.ClockCompleted": () => {
        queryClient.invalidateQueries({
          queryKey: ["record-clocks"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationCreated": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
        queryClient.invalidateQueries({
          queryKey: ["notifications-unread-count"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationQueued": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationRead": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
        queryClient.invalidateQueries({
          queryKey: ["notifications-unread-count"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.AllNotificationsRead": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
        queryClient.invalidateQueries({
          queryKey: ["notifications-unread-count"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationDelivered": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
        queryClient.invalidateQueries({
          queryKey: ["notifications-unread-count"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationDeliveryFailed": () => {
        queryClient.invalidateQueries({
          queryKey: ["notification"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationBounced": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
      },
      "Records.Notifications.Domain.Events.NotificationCancelled": () => {
        queryClient.invalidateQueries({
          queryKey: ["notifications"],
          exact: false,
        });
      },
    });

    const { close } = connectChangeFeed(handler, {
      withCredentials: true,
      onError: () => {
        // Optionally add a toast or log
      },
    });

    return () => close();
  }, [auth.status, queryClient]);

  if (auth.status === "checking") {
    return (
      <Box
        sx={{
          minHeight: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (auth.status !== "loggedIn") {
    const callbackUrl = `${location.pathname}${location.search}${location.hash}`;
    const search = callbackUrl
      ? `?callbackUrl=${encodeURIComponent(callbackUrl)}`
      : "";
    return <Navigate to={`/sign-in${search}`} replace />;
  }

  const canManageGlobalAdminAreas = auth.access.isGlobalAdmin;
  const sectionLabel = getSectionLabel(location.pathname);
  const pageLabel = getPageLabel(location.pathname);

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        backgroundColor: "background.default",
      }}
    >
      <AppSidebar
        canManageGlobalAdminAreas={canManageGlobalAdminAreas}
        collapsed={sidebarCollapsed}
        mobileOpen={mobileSidebarOpen}
        onCloseMobile={() => setMobileSidebarOpen(false)}
        onToggleCollapse={() =>
          setSidebarCollapsed((currentValue) => !currentValue)
        }
      />

      <AppBar
        position="fixed"
        sx={{
          backgroundColor: "background.paper",
          color: "text.primary",
          boxShadow: "none",
          borderBottom: 1,
          borderColor: "divider",
          width: {
            xs: "100%",
            lg: sidebarCollapsed
              ? `calc(100% - ${collapsedSidebarWidth}px)`
              : `calc(100% - ${expandedSidebarWidth}px)`,
          },
          ml: {
            xs: 0,
            lg: sidebarCollapsed
              ? `${collapsedSidebarWidth}px`
              : `${expandedSidebarWidth}px`,
          },
        }}
      >
        <Toolbar
          sx={{
            minHeight: 72,
            px: { xs: 2, md: 3 },
            display: "flex",
            justifyContent: "space-between",
            gap: 2,
          }}
        >
          <Stack direction="row" spacing={1.5} alignItems="center">
            <IconButton
              onClick={() => setMobileSidebarOpen(true)}
              sx={{ display: { xs: "inline-flex", lg: "none" } }}
            >
              <MenuIcon />
            </IconButton>

            <Box sx={{ minWidth: 0 }}>
              <Typography
                variant="overline"
                sx={{ color: "text.secondary", letterSpacing: "0.14em" }}
              >
                {sectionLabel}
              </Typography>
              <Typography variant="h6" sx={{ fontWeight: 700 }} noWrap>
                {pageLabel}
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1.5} alignItems="center">
            <NotificationsBell />
            <UserMenu />
          </Stack>
        </Toolbar>
      </AppBar>

      <Box
        component="main"
        sx={{
          flex: 1,
          minWidth: 0,
        }}
      >
        <Toolbar sx={{ minHeight: 72 }} />
        <Container maxWidth="xl" sx={{ py: { xs: 3, md: 4 } }}>
          <Outlet />
        </Container>
      </Box>
    </Box>
  );
};

export default Shell;
