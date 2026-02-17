import * as React from "react";

import { Block, PersonAdd, Security } from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import {
  useMutation,
  useQuery,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";

import { useAuth } from "../auth";
import { Titlebar } from "../components";
import { resolveActorUlid } from "../lib/identifiers";
import { UserRole, UserSummary } from "../models";
import {
  tenantSummariesQueryOptions,
  userRoleMembershipsQueryOptions,
  userSummariesQueryOptions,
} from "../query-options";

const roleDisplayNames: Record<UserRole, string> = {
  GlobalAdmin: "Global Admin",
  TenantAdmin: "Tenant Admin",
  Operations: "Operations",
};

const formatTimestamp = (value: string) => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
};

const getInitialTenantId = (tenantIds: string[]) => tenantIds[0] ?? "";

const UsersAdminPage = () => {
  const auth = useAuth();
  const actorId = resolveActorUlid(auth.user);
  const queryClient = useQueryClient();

  const usersQuery = useSuspenseQuery(userSummariesQueryOptions());
  const tenantsQuery = useSuspenseQuery(tenantSummariesQueryOptions());
  const tenantIds = tenantsQuery.data.map((tenant) => tenant.tenantId);
  const fallbackTenantId = React.useMemo(
    () => getInitialTenantId(tenantIds),
    [tenantIds],
  );

  const [selectedUserId, setSelectedUserId] = React.useState<string | null>(
    usersQuery.data[0]?.userId ?? null,
  );

  React.useEffect(() => {
    if (
      selectedUserId &&
      usersQuery.data.some((user) => user.userId === selectedUserId)
    ) {
      return;
    }
    setSelectedUserId(usersQuery.data[0]?.userId ?? null);
  }, [selectedUserId, usersQuery.data]);

  const selectedUser = React.useMemo(
    () =>
      usersQuery.data.find((user) => user.userId === selectedUserId) ?? null,
    [selectedUserId, usersQuery.data],
  );

  const userRolesQuery = useQuery(
    userRoleMembershipsQueryOptions(selectedUser?.userId ?? undefined),
  );

  const [inviteEmail, setInviteEmail] = React.useState("");
  const [inviteDisplayName, setInviteDisplayName] = React.useState("");
  const [inviteRole, setInviteRole] = React.useState<UserRole>("Operations");
  const [inviteTenantId, setInviteTenantId] = React.useState(fallbackTenantId);
  const [inviteError, setInviteError] = React.useState<string | null>(null);

  const [grantRole, setGrantRole] = React.useState<UserRole>("Operations");
  const [grantTenantId, setGrantTenantId] = React.useState(fallbackTenantId);
  const [grantError, setGrantError] = React.useState<string | null>(null);
  const [suspendError, setSuspendError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (inviteTenantId && tenantIds.includes(inviteTenantId)) {
      return;
    }
    setInviteTenantId(fallbackTenantId);
  }, [fallbackTenantId, inviteTenantId, tenantIds]);

  React.useEffect(() => {
    if (grantTenantId && tenantIds.includes(grantTenantId)) {
      return;
    }
    setGrantTenantId(fallbackTenantId);
  }, [fallbackTenantId, grantTenantId, tenantIds]);

  const inviteUserMutation = useMutation({
    mutationFn: async (payload: {
      email: string;
      displayName?: string;
      role: UserRole;
      tenantId?: string;
    }) => {
      const response = await fetch("/api/users/invite", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: payload.email,
          displayName: payload.displayName,
          roles: [
            {
              role: payload.role,
              tenantId: payload.tenantId ?? null,
            },
          ],
          invitedAt: new Date().toISOString(),
        }),
      });

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to invite user");
        throw new Error(message);
      }

      return (await response.json()) as UserSummary;
    },
    onSuccess: async (createdUser) => {
      setInviteEmail("");
      setInviteDisplayName("");
      setInviteRole("Operations");
      setInviteTenantId(fallbackTenantId);
      setSelectedUserId(createdUser.userId);
      await queryClient.invalidateQueries({
        queryKey: ["user-summaries"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["user-role-memberships"],
        exact: false,
      });
    },
  });

  const suspendUserMutation = useMutation({
    mutationFn: async (user: UserSummary) => {
      const response = await fetch(`/api/users/${user.userId}/suspend`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId: user.userId,
          reason: null,
          suspendedAt: new Date().toISOString(),
        }),
      });

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to suspend user");
        throw new Error(message);
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["user-summaries"],
      });
    },
  });

  const grantRoleMutation = useMutation({
    mutationFn: async (payload: {
      userId: string;
      role: UserRole;
      tenantId?: string;
    }) => {
      const response = await fetch(`/api/users/${payload.userId}/roles/grant`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userId: payload.userId,
          role: payload.role,
          tenantId: payload.tenantId ?? null,
          grantedBy: actorId,
          grantedAt: new Date().toISOString(),
        }),
      });

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to grant role");
        throw new Error(message);
      }
    },
    onSuccess: async (_, payload) => {
      await queryClient.invalidateQueries({
        queryKey: ["user-role-memberships", payload.userId],
      });
    },
  });

  const revokeRoleMutation = useMutation({
    mutationFn: async (payload: {
      userId: string;
      role: UserRole;
      tenantId?: string;
    }) => {
      const response = await fetch(
        `/api/users/${payload.userId}/roles/revoke`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            userId: payload.userId,
            role: payload.role,
            tenantId: payload.tenantId ?? null,
            revokedBy: actorId,
          }),
        },
      );

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to revoke role");
        throw new Error(message);
      }
    },
    onSuccess: async (_, payload) => {
      await queryClient.invalidateQueries({
        queryKey: ["user-role-memberships", payload.userId],
      });
    },
  });

  const handleInviteSubmit = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();
    setInviteError(null);

    const trimmedEmail = inviteEmail.trim();
    const trimmedDisplayName = inviteDisplayName.trim();

    if (trimmedEmail.length === 0) {
      setInviteError("Email is required.");
      return;
    }

    if (inviteRole === "TenantAdmin" && !inviteTenantId) {
      setInviteError("Tenant Admin role requires a tenant.");
      return;
    }

    const inviteScopeTenantId =
      inviteRole === "GlobalAdmin" ? undefined : inviteTenantId || undefined;

    try {
      await inviteUserMutation.mutateAsync({
        email: trimmedEmail,
        displayName: trimmedDisplayName || undefined,
        role: inviteRole,
        tenantId: inviteScopeTenantId,
      });
    } catch (error) {
      setInviteError(
        error instanceof Error ? error.message : "Failed to invite user.",
      );
    }
  };

  const handleSuspendUser = async (user: UserSummary) => {
    setSuspendError(null);
    try {
      await suspendUserMutation.mutateAsync(user);
    } catch (error) {
      setSuspendError(
        error instanceof Error ? error.message : "Failed to suspend user.",
      );
    }
  };

  const handleSuspendUserClick = (user: UserSummary) => {
    handleSuspendUser(user).catch(() => undefined);
  };

  const handleGrantRole = async () => {
    if (!selectedUser) {
      return;
    }

    setGrantError(null);
    if (grantRole === "TenantAdmin" && !grantTenantId) {
      setGrantError("Tenant Admin role requires a tenant.");
      return;
    }

    const grantScopeTenantId =
      grantRole === "GlobalAdmin" ? undefined : grantTenantId || undefined;

    try {
      await grantRoleMutation.mutateAsync({
        userId: selectedUser.userId,
        role: grantRole,
        tenantId: grantScopeTenantId,
      });
    } catch (error) {
      setGrantError(
        error instanceof Error ? error.message : "Failed to grant role.",
      );
    }
  };

  const handleGrantRoleClick = () => {
    handleGrantRole().catch(() => undefined);
  };

  const handleRevokeRole = async (role: UserRole, tenantId?: string) => {
    if (!selectedUser) {
      return;
    }

    setGrantError(null);
    try {
      await revokeRoleMutation.mutateAsync({
        userId: selectedUser.userId,
        role,
        tenantId,
      });
    } catch (error) {
      setGrantError(
        error instanceof Error ? error.message : "Failed to revoke role.",
      );
    }
  };

  const handleRevokeRoleClick = (role: UserRole, tenantId?: string) => {
    handleRevokeRole(role, tenantId).catch(() => undefined);
  };

  const inviteTenantDisabled = inviteRole === "GlobalAdmin";
  const inviteTenantValue = inviteTenantDisabled ? "" : inviteTenantId;
  const grantTenantDisabled = grantRole === "GlobalAdmin";
  const grantTenantValue = grantTenantDisabled ? "" : grantTenantId;

  const inviteErrorAlert = inviteError ? (
    <Alert severity="error" sx={{ mt: 2 }}>
      {inviteError}
    </Alert>
  ) : null;

  const usersEmptyState =
    usersQuery.data.length === 0 ? (
      <Box sx={{ px: 3, py: 4 }}>
        <Typography color="text.secondary">No users found.</Typography>
      </Box>
    ) : null;

  const suspendErrorAlert = suspendError ? (
    <Alert severity="error">{suspendError}</Alert>
  ) : null;

  const selectedUserCaption = selectedUser ? (
    <Typography variant="body2" color="text.secondary">
      {selectedUser.email}
    </Typography>
  ) : null;

  const grantErrorAlert = grantError ? (
    <Alert severity="error">{grantError}</Alert>
  ) : null;

  const memberships = userRolesQuery.data ?? [];
  const roleRows = memberships.map((membership) => (
    <TableRow key={`${membership.role}:${membership.tenantId ?? "global"}`}>
      <TableCell>{roleDisplayNames[membership.role]}</TableCell>
      <TableCell>{membership.tenantId ?? "Global"}</TableCell>
      <TableCell>{formatTimestamp(membership.grantedAt)}</TableCell>
      <TableCell align="right">
        <Button
          size="small"
          color="error"
          variant="outlined"
          disabled={revokeRoleMutation.isPending}
          onClick={() =>
            handleRevokeRoleClick(membership.role, membership.tenantId)
          }
        >
          Revoke
        </Button>
      </TableCell>
    </TableRow>
  ));

  const noRoleMembershipRow =
    memberships.length === 0 ? (
      <TableRow>
        <TableCell colSpan={4}>
          <Typography color="text.secondary">
            No role memberships for this user.
          </Typography>
        </TableCell>
      </TableRow>
    ) : null;

  let roleMembershipsContent: React.ReactNode = (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>Role</TableCell>
          <TableCell>Tenant</TableCell>
          <TableCell>Granted</TableCell>
          <TableCell align="right">Actions</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {roleRows}
        {noRoleMembershipRow}
      </TableBody>
    </Table>
  );

  if (!selectedUser) {
    roleMembershipsContent = (
      <Typography color="text.secondary">
        Select a user to manage role memberships.
      </Typography>
    );
  } else if (userRolesQuery.isLoading) {
    roleMembershipsContent = (
      <Typography color="text.secondary">Loading roles...</Typography>
    );
  }

  return (
    <Stack spacing={3}>
      <Titlebar title="Users Admin" />

      <Paper sx={{ p: 3 }}>
        <Stack
          component="form"
          spacing={2}
          onSubmit={handleInviteSubmit}
          direction={{ xs: "column", lg: "row" }}
          alignItems={{ xs: "stretch", lg: "center" }}
        >
          <TextField
            label="Email"
            type="email"
            value={inviteEmail}
            onChange={(event) => setInviteEmail(event.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Display name"
            value={inviteDisplayName}
            onChange={(event) => setInviteDisplayName(event.target.value)}
            fullWidth
          />
          <FormControl sx={{ minWidth: 180 }}>
            <InputLabel id="invite-role-label">Role</InputLabel>
            <Select
              labelId="invite-role-label"
              value={inviteRole}
              label="Role"
              onChange={(event) =>
                setInviteRole(event.target.value as UserRole)
              }
            >
              <MenuItem value="GlobalAdmin">Global Admin</MenuItem>
              <MenuItem value="TenantAdmin">Tenant Admin</MenuItem>
              <MenuItem value="Operations">Operations</MenuItem>
            </Select>
          </FormControl>
          <FormControl sx={{ minWidth: 220 }} disabled={inviteTenantDisabled}>
            <InputLabel id="invite-tenant-label">Tenant</InputLabel>
            <Select
              labelId="invite-tenant-label"
              value={inviteTenantValue}
              label="Tenant"
              onChange={(event) =>
                setInviteTenantId(String(event.target.value))
              }
            >
              {tenantsQuery.data.map((tenant) => (
                <MenuItem key={tenant.tenantId} value={tenant.tenantId}>
                  {tenant.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button
            type="submit"
            variant="contained"
            startIcon={<PersonAdd />}
            disabled={inviteUserMutation.isPending}
          >
            Invite user
          </Button>
        </Stack>
        {inviteErrorAlert}
      </Paper>

      <Paper sx={{ p: 0 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>User</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {usersQuery.data.map((user) => {
              const isSelected = user.userId === selectedUserId;
              const userLabel = user.displayName
                ? user.displayName
                : user.email;
              const statusColor =
                user.status === "Suspended" ? "warning" : "default";
              const disableSuspendAction =
                user.status === "Suspended" || suspendUserMutation.isPending;

              return (
                <TableRow
                  key={user.userId}
                  hover
                  selected={isSelected}
                  onClick={() => setSelectedUserId(user.userId)}
                  sx={{ cursor: "pointer" }}
                >
                  <TableCell>
                    <Stack>
                      <Typography variant="body2" fontWeight={600}>
                        {userLabel}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {user.email}
                      </Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={user.status}
                      color={statusColor}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Button
                      variant="outlined"
                      color="warning"
                      size="small"
                      startIcon={<Block />}
                      disabled={disableSuspendAction}
                      onClick={(event) => {
                        event.stopPropagation();
                        handleSuspendUserClick(user);
                      }}
                    >
                      Suspend
                    </Button>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
        {usersEmptyState}
      </Paper>

      {suspendErrorAlert}

      <Paper sx={{ p: 3 }}>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={2}
            alignItems={{ xs: "stretch", md: "center" }}
          >
            <Typography variant="h6" sx={{ flexGrow: 1 }}>
              Role Memberships
            </Typography>
            {selectedUserCaption}
          </Stack>

          <Stack
            spacing={2}
            direction={{ xs: "column", lg: "row" }}
            alignItems={{ xs: "stretch", lg: "center" }}
          >
            <FormControl sx={{ minWidth: 180 }}>
              <InputLabel id="grant-role-label">Role</InputLabel>
              <Select
                labelId="grant-role-label"
                value={grantRole}
                label="Role"
                onChange={(event) =>
                  setGrantRole(event.target.value as UserRole)
                }
              >
                <MenuItem value="GlobalAdmin">Global Admin</MenuItem>
                <MenuItem value="TenantAdmin">Tenant Admin</MenuItem>
                <MenuItem value="Operations">Operations</MenuItem>
              </Select>
            </FormControl>
            <FormControl sx={{ minWidth: 220 }} disabled={grantTenantDisabled}>
              <InputLabel id="grant-tenant-label">Tenant</InputLabel>
              <Select
                labelId="grant-tenant-label"
                value={grantTenantValue}
                label="Tenant"
                onChange={(event) =>
                  setGrantTenantId(String(event.target.value))
                }
              >
                {tenantsQuery.data.map((tenant) => (
                  <MenuItem key={tenant.tenantId} value={tenant.tenantId}>
                    {tenant.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button
              variant="contained"
              startIcon={<Security />}
              disabled={!selectedUser || grantRoleMutation.isPending}
              onClick={handleGrantRoleClick}
            >
              Grant role
            </Button>
          </Stack>

          {grantErrorAlert}
          {roleMembershipsContent}
        </Stack>
      </Paper>
    </Stack>
  );
};

export default UsersAdminPage;
