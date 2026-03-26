import * as React from "react";

import { Block, PersonAdd, Security } from "@mui/icons-material";
import {
  Alert,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
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
import { ConfirmDeleteDialog, PageSection, Titlebar } from "../components";
import { resolveActorUlid } from "../lib/identifiers";
import { TenantSummary, UserRole, UserSummary } from "../models";
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

const roleOrder: Record<UserRole, number> = {
  GlobalAdmin: 0,
  TenantAdmin: 1,
  Operations: 2,
};

const availableRoles: UserRole[] = ["GlobalAdmin", "TenantAdmin", "Operations"];

const formatTimestamp = (value: string) => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
};

const getInitialTenantId = (tenantIds: string[]) => tenantIds[0] ?? "";

const getUserLabel = (user: UserSummary) => {
  const displayName = user.displayName?.trim();
  return displayName && displayName.length > 0 ? displayName : user.email;
};

const compareUsers = (left: UserSummary, right: UserSummary) => {
  const labelComparison = getUserLabel(left).localeCompare(getUserLabel(right));
  if (labelComparison !== 0) {
    return labelComparison;
  }

  return left.email.localeCompare(right.email);
};

const compareTenants = (left: TenantSummary, right: TenantSummary) =>
  left.name.localeCompare(right.name);

interface RoleMembershipSelection {
  userId: string;
  userLabel: string;
  role: UserRole;
  tenantId?: string;
}

const UsersAdminPage = () => {
  const auth = useAuth();
  const actorId = resolveActorUlid(auth.user);
  const queryClient = useQueryClient();

  const usersQuery = useSuspenseQuery(userSummariesQueryOptions());
  const tenantsQuery = useSuspenseQuery(tenantSummariesQueryOptions());
  const sortedUsers = React.useMemo(
    () => [...usersQuery.data].sort(compareUsers),
    [usersQuery.data],
  );
  const sortedTenants = React.useMemo(
    () => [...tenantsQuery.data].sort(compareTenants),
    [tenantsQuery.data],
  );
  const tenantIds = sortedTenants.map((tenant) => tenant.tenantId);
  const tenantNamesById = React.useMemo(
    () =>
      new Map(sortedTenants.map((tenant) => [tenant.tenantId, tenant.name])),
    [sortedTenants],
  );
  const fallbackTenantId = React.useMemo(
    () => getInitialTenantId(tenantIds),
    [tenantIds],
  );

  const [selectedUserId, setSelectedUserId] = React.useState<string | null>(
    sortedUsers[0]?.userId ?? null,
  );

  React.useEffect(() => {
    if (
      selectedUserId &&
      sortedUsers.some((user) => user.userId === selectedUserId)
    ) {
      return;
    }
    setSelectedUserId(sortedUsers[0]?.userId ?? null);
  }, [selectedUserId, sortedUsers]);

  const selectedUser = React.useMemo(
    () => sortedUsers.find((user) => user.userId === selectedUserId) ?? null,
    [selectedUserId, sortedUsers],
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
  const [userToSuspend, setUserToSuspend] = React.useState<UserSummary | null>(
    null,
  );
  const [roleMembershipToRevoke, setRoleMembershipToRevoke] =
    React.useState<RoleMembershipSelection | null>(null);

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
    setUserToSuspend(user);
  };

  const handleConfirmSuspendUser = async () => {
    if (!userToSuspend) {
      return;
    }

    try {
      await handleSuspendUser(userToSuspend);
    } finally {
      setUserToSuspend(null);
    }
  };

  const handleCancelSuspendUser = () => {
    setUserToSuspend(null);
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

  const handleRevokeRole = async (membership: RoleMembershipSelection) => {
    setGrantError(null);
    try {
      await revokeRoleMutation.mutateAsync({
        userId: membership.userId,
        role: membership.role,
        tenantId: membership.tenantId,
      });
    } catch (error) {
      setGrantError(
        error instanceof Error ? error.message : "Failed to revoke role.",
      );
    }
  };

  const handleRevokeRoleClick = (membership: RoleMembershipSelection) => {
    setRoleMembershipToRevoke(membership);
  };

  const handleConfirmRevokeRole = async () => {
    if (!roleMembershipToRevoke) {
      return;
    }

    try {
      await handleRevokeRole(roleMembershipToRevoke);
    } finally {
      setRoleMembershipToRevoke(null);
    }
  };

  const handleCancelRevokeRole = () => {
    setRoleMembershipToRevoke(null);
  };

  const inviteTenantDisabled =
    inviteRole === "GlobalAdmin" || sortedTenants.length === 0;
  const inviteTenantValue = inviteTenantDisabled ? "" : inviteTenantId;
  const grantTenantDisabled =
    grantRole === "GlobalAdmin" || sortedTenants.length === 0;
  const grantTenantValue = grantTenantDisabled ? "" : grantTenantId;

  const inviteErrorAlert = inviteError ? (
    <Alert severity="error">{inviteError}</Alert>
  ) : null;

  const usersEmptyState =
    sortedUsers.length === 0 ? (
      <Typography color="text.secondary">No users found.</Typography>
    ) : null;

  const suspendErrorAlert = suspendError ? (
    <Alert severity="error">{suspendError}</Alert>
  ) : null;

  const selectedUserCaption = selectedUser ? (
    <Stack spacing={0.25} alignItems={{ xs: "flex-start", md: "flex-end" }}>
      <Typography variant="body2" fontWeight={600}>
        {getUserLabel(selectedUser)}
      </Typography>
      <Typography variant="caption" color="text.secondary">
        {selectedUser.email}
      </Typography>
    </Stack>
  ) : null;

  const grantErrorAlert = grantError ? (
    <Alert severity="error">{grantError}</Alert>
  ) : null;

  const sortedMemberships = React.useMemo(() => {
    const memberships = userRolesQuery.data ?? [];

    return [...memberships].sort((left, right) => {
      const roleComparison = roleOrder[left.role] - roleOrder[right.role];
      if (roleComparison !== 0) {
        return roleComparison;
      }

      const leftTenantLabel = left.tenantId
        ? (tenantNamesById.get(left.tenantId) ?? left.tenantId)
        : "Global";
      const rightTenantLabel = right.tenantId
        ? (tenantNamesById.get(right.tenantId) ?? right.tenantId)
        : "Global";
      return leftTenantLabel.localeCompare(rightTenantLabel);
    });
  }, [tenantNamesById, userRolesQuery.data]);

  const tenantAvailabilityAlert =
    sortedTenants.length === 0 ? (
      <Alert severity="info">
        Create a tenant to target tenant-scoped access.
      </Alert>
    ) : null;

  const roleRows = sortedMemberships.map((membership) => {
    const tenantCell = membership.tenantId ? (
      <Stack spacing={0.25}>
        <Typography variant="body2">
          {tenantNamesById.get(membership.tenantId) ?? membership.tenantId}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {membership.tenantId}
        </Typography>
      </Stack>
    ) : (
      "Global"
    );

    const handleRevokeClick = () => {
      if (!selectedUser) {
        return;
      }

      handleRevokeRoleClick({
        userId: selectedUser.userId,
        userLabel: getUserLabel(selectedUser),
        role: membership.role,
        tenantId: membership.tenantId,
      });
    };

    return (
      <TableRow key={`${membership.role}:${membership.tenantId ?? "global"}`}>
        <TableCell>{roleDisplayNames[membership.role]}</TableCell>
        <TableCell>{tenantCell}</TableCell>
        <TableCell>{formatTimestamp(membership.grantedAt)}</TableCell>
        <TableCell align="right">
          <Button
            size="small"
            color="error"
            variant="outlined"
            disabled={revokeRoleMutation.isPending}
            onClick={handleRevokeClick}
          >
            Revoke
          </Button>
        </TableCell>
      </TableRow>
    );
  });

  const noRoleMembershipRow =
    sortedMemberships.length === 0 ? (
      <TableRow>
        <TableCell colSpan={4}>
          <Typography color="text.secondary">
            No role memberships for this user.
          </Typography>
        </TableCell>
      </TableRow>
    ) : null;

  let roleMembershipsContent: React.ReactNode = (
    <TableContainer>
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
    </TableContainer>
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
  } else if (userRolesQuery.isError) {
    roleMembershipsContent = (
      <Alert severity="error">
        {userRolesQuery.error.message || "Failed to load role memberships."}
      </Alert>
    );
  }

  const suspendUserDialogDescription = userToSuspend
    ? `Suspend ${getUserLabel(userToSuspend)}? They will lose access until reactivated.`
    : "Suspend this user?";
  const revokeRoleDialogDescription = roleMembershipToRevoke
    ? `Revoke ${roleDisplayNames[roleMembershipToRevoke.role]} access from ${roleMembershipToRevoke.userLabel}${
        roleMembershipToRevoke.tenantId
          ? ` for ${tenantNamesById.get(roleMembershipToRevoke.tenantId) ?? roleMembershipToRevoke.tenantId}`
          : ""
      }?`
    : "Revoke this role?";

  return (
    <Stack spacing={3}>
      <Titlebar title="Users" />

      <PageSection
        title="Invite user"
        description="Send an invitation and assign the user's starting access."
      >
        <Stack spacing={2}>
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
                {availableRoles.map((role) => (
                  <MenuItem key={role} value={role}>
                    {roleDisplayNames[role]}
                  </MenuItem>
                ))}
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
                {sortedTenants.map((tenant) => (
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
          {tenantAvailabilityAlert}
          {inviteErrorAlert}
        </Stack>
      </PageSection>

      <PageSection
        title="User directory"
        description="Select a user to review their access or suspend their account."
      >
        <Stack spacing={2}>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>User</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sortedUsers.map((user) => {
                  const isSelected = user.userId === selectedUserId;
                  const statusColor =
                    user.status === "Suspended" ? "warning" : "default";
                  const disableSuspendAction =
                    user.status === "Suspended" ||
                    suspendUserMutation.isPending;

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
                            {getUserLabel(user)}
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
          </TableContainer>
          {usersEmptyState}
        </Stack>
      </PageSection>

      {suspendErrorAlert}

      <PageSection
        title="Role memberships"
        description="Grant or revoke access for the selected user."
        actions={selectedUserCaption}
      >
        <Stack spacing={2}>
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
                {availableRoles.map((role) => (
                  <MenuItem key={role} value={role}>
                    {roleDisplayNames[role]}
                  </MenuItem>
                ))}
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
                {sortedTenants.map((tenant) => (
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

          {tenantAvailabilityAlert}
          {grantErrorAlert}
          {roleMembershipsContent}
        </Stack>
      </PageSection>

      <ConfirmDeleteDialog
        open={Boolean(userToSuspend)}
        title="Suspend user"
        description={suspendUserDialogDescription}
        confirmLabel="Suspend user"
        confirmColor="warning"
        confirmDisabled={suspendUserMutation.isPending}
        onCancel={handleCancelSuspendUser}
        onConfirm={handleConfirmSuspendUser}
      />

      <ConfirmDeleteDialog
        open={Boolean(roleMembershipToRevoke)}
        title="Revoke role"
        description={revokeRoleDialogDescription}
        confirmLabel="Revoke role"
        confirmDisabled={revokeRoleMutation.isPending}
        onCancel={handleCancelRevokeRole}
        onConfirm={handleConfirmRevokeRole}
      />
    </Stack>
  );
};

export default UsersAdminPage;
