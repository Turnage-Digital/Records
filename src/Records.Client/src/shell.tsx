import * as React from "react";

import {
  AppBar,
  Box,
  CircularProgress,
  Container,
  Tab,
  Tabs,
  Toolbar,
  Typography,
  useTheme,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import {
  Link as RouterLink,
  Navigate,
  Outlet,
  useLocation,
} from "react-router-dom";

import { useAuth } from "./auth";
import { NotificationsBell, UserMenu } from "./components";
import { connectChangeFeed, createChangeFeedRouter } from "./lib/sse";

const Shell = () => {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const theme = useTheme();
  const location = useLocation();

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

  let selectedNav: "recordsets" | "tenants" | "users" = "recordsets";
  if (location.pathname.startsWith("/admin/tenants")) {
    selectedNav = "tenants";
  } else if (location.pathname.startsWith("/admin/users")) {
    selectedNav = "users";
  }

  return (
    <>
      <Box sx={{ minHeight: "100vh" }}>
        <AppBar
          position="fixed"
          sx={{
            backgroundColor: theme.palette.background.paper,
            color: theme.palette.text.primary,
            boxShadow: theme.shadows[1],
            borderBottom: `1px solid ${theme.palette.divider}`,
          }}
        >
          <Toolbar>
            <Typography
              component="h1"
              variant="h4"
              fontWeight="bold"
              color="primary"
              sx={{ mr: 3 }}
            >
              Records
            </Typography>

            <Tabs
              value={selectedNav}
              textColor="primary"
              indicatorColor="primary"
              sx={{ flexGrow: 1, minHeight: 48 }}
            >
              <Tab
                value="recordsets"
                label="Recordsets"
                component={RouterLink}
                to="/"
                sx={{ minHeight: 48, textTransform: "none" }}
              />
              {canManageGlobalAdminAreas ? (
                <Tab
                  value="tenants"
                  label="Tenants"
                  component={RouterLink}
                  to="/admin/tenants"
                  sx={{ minHeight: 48, textTransform: "none" }}
                />
              ) : null}
              {canManageGlobalAdminAreas ? (
                <Tab
                  value="users"
                  label="Users"
                  component={RouterLink}
                  to="/admin/users"
                  sx={{ minHeight: 48, textTransform: "none" }}
                />
              ) : null}
            </Tabs>

            <NotificationsBell />
            <UserMenu />
          </Toolbar>
        </AppBar>

        <Container
          component="main"
          maxWidth="xl"
          sx={{
            minHeight: "100vh",
            backgroundColor: theme.palette.background.default,
            py: 4,
          }}
        >
          <Box sx={(muiTheme) => ({ ...muiTheme.mixins.toolbar })} />
          <Outlet />
        </Container>
      </Box>
    </>
  );
};

export default Shell;
