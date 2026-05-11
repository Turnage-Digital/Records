import * as React from "react";

import { AddBusiness, Block } from "@mui/icons-material";
import {
  Alert,
  Button,
  Chip,
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
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";

import ConfirmDeleteDialog from "../components/confirm-delete-dialog";
import PageSection from "../components/page-section";
import Titlebar from "../components/titlebar";
import { tenantSummariesQueryOptions } from "../query-options";

import type { TenantSummary } from "../models/tenant-summary";

const formatTimestamp = (value: string) => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
};

const compareTenants = (left: TenantSummary, right: TenantSummary) => {
  const createdComparison = right.createdAt.localeCompare(left.createdAt);
  if (createdComparison !== 0) {
    return createdComparison;
  }

  return left.name.localeCompare(right.name);
};

const TenantsAdminPage = () => {
  const queryClient = useQueryClient();
  const tenantsQuery = useSuspenseQuery(tenantSummariesQueryOptions());

  const [newTenantName, setNewTenantName] = React.useState("");
  const [createError, setCreateError] = React.useState<string | null>(null);
  const [disableError, setDisableError] = React.useState<string | null>(null);
  const [tenantToDisable, setTenantToDisable] =
    React.useState<TenantSummary | null>(null);

  const createTenantMutation = useMutation({
    mutationFn: async (name: string) => {
      const response = await fetch("/api/tenants", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ name }),
      });

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to create tenant");
        throw new Error(message);
      }
    },
    onSuccess: async () => {
      setNewTenantName("");
      await queryClient.invalidateQueries({
        queryKey: ["tenant-summaries"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["clock-definitions"],
        exact: false,
      });
    },
  });

  const disableTenantMutation = useMutation({
    mutationFn: async (tenantId: string) => {
      const response = await fetch(`/api/tenants/${tenantId}/disable`, {
        method: "POST",
      });

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to disable tenant");
        throw new Error(message);
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["tenant-summaries"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["clock-definitions"],
        exact: false,
      });
    },
  });

  const sortedTenants = React.useMemo(
    () => [...tenantsQuery.data].sort(compareTenants),
    [tenantsQuery.data],
  );

  const handleCreateTenant = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();
    setCreateError(null);

    const trimmed = newTenantName.trim();
    if (trimmed.length < 2) {
      setCreateError("Tenant name must be at least 2 characters.");
      return;
    }

    try {
      await createTenantMutation.mutateAsync(trimmed);
    } catch (error) {
      setCreateError(
        error instanceof Error ? error.message : "Failed to create tenant.",
      );
    }
  };

  const handleDisableTenant = async (tenant: TenantSummary) => {
    setDisableError(null);
    try {
      await disableTenantMutation.mutateAsync(tenant.tenantId);
    } catch (error) {
      setDisableError(
        error instanceof Error ? error.message : "Failed to disable tenant.",
      );
    }
  };

  const handleDisableTenantClick = (tenant: TenantSummary) => {
    setTenantToDisable(tenant);
  };

  const handleConfirmDisableTenant = async () => {
    if (!tenantToDisable) {
      return;
    }

    try {
      await handleDisableTenant(tenantToDisable);
    } finally {
      setTenantToDisable(null);
    }
  };

  const handleCancelDisableTenant = () => {
    setTenantToDisable(null);
  };

  const createErrorAlert = createError ? (
    <Alert severity="error" aria-live="assertive" sx={{ mt: 2 }}>
      {createError}
    </Alert>
  ) : null;

  const disableErrorAlert = disableError ? (
    <Alert severity="error" aria-live="assertive">
      {disableError}
    </Alert>
  ) : null;

  const noTenantsMessage =
    sortedTenants.length === 0 ? (
      <Typography color="text.secondary">No tenants created yet.</Typography>
    ) : null;

  const disableTenantDialogDescription = tenantToDisable
    ? `Disable tenant "${tenantToDisable.name}"? Users and tenant-scoped areas will lose access immediately.`
    : "Disable this tenant?";

  return (
    <Stack spacing={3}>
      <Titlebar title="Tenants" />

      <PageSection
        title="Create tenant"
        description="Add a tenant before assigning tenant-scoped users, clocks, and records."
      >
        <Stack
          component="form"
          spacing={2}
          direction={{ xs: "column", md: "row" }}
          onSubmit={handleCreateTenant}
          alignItems={{ xs: "stretch", md: "center" }}
        >
          <TextField
            label="Tenant name"
            value={newTenantName}
            onChange={(event) => setNewTenantName(event.target.value)}
            fullWidth
            required
          />
          <Button
            type="submit"
            variant="contained"
            startIcon={<AddBusiness />}
            disabled={createTenantMutation.isPending}
          >
            Create tenant
          </Button>
        </Stack>
        {createErrorAlert}
      </PageSection>

      <PageSection
        title="Tenant directory"
        description="Review tenant status and disable tenant access when needed."
      >
        <Stack spacing={2}>
          <TableContainer>
            <Table size="small" aria-label="Tenant directory">
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Created</TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {sortedTenants.map((tenant) => {
                  const canDisable = tenant.status === "Active";
                  const statusColor = canDisable ? "success" : "default";
                  return (
                    <TableRow key={tenant.tenantId} hover>
                      <TableCell>
                        <Stack>
                          <Typography variant="body2" fontWeight={600}>
                            {tenant.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {tenant.tenantId}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={statusColor}
                          label={tenant.status}
                        />
                      </TableCell>
                      <TableCell>{formatTimestamp(tenant.createdAt)}</TableCell>
                      <TableCell align="right">
                        <Button
                          variant="outlined"
                          color="error"
                          size="small"
                          startIcon={<Block />}
                          disabled={
                            !canDisable || disableTenantMutation.isPending
                          }
                          onClick={() => handleDisableTenantClick(tenant)}
                        >
                          Disable
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
          {noTenantsMessage}
        </Stack>
      </PageSection>

      {disableErrorAlert}

      <ConfirmDeleteDialog
        open={Boolean(tenantToDisable)}
        title="Disable tenant"
        description={disableTenantDialogDescription}
        confirmLabel="Disable tenant"
        confirmDisabled={disableTenantMutation.isPending}
        onCancel={handleCancelDisableTenant}
        onConfirm={handleConfirmDisableTenant}
      />
    </Stack>
  );
};

export default TenantsAdminPage;
