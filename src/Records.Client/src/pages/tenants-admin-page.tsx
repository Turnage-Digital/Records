import * as React from "react";

import { AddBusiness, Block } from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
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
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";

import { Titlebar } from "../components";
import { TenantSummary } from "../models";
import { tenantSummariesQueryOptions } from "../query-options";

const formatTimestamp = (value: string) => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return parsed.toLocaleString();
};

const TenantsAdminPage = () => {
  const queryClient = useQueryClient();
  const tenantsQuery = useSuspenseQuery(tenantSummariesQueryOptions());

  const [newTenantName, setNewTenantName] = React.useState("");
  const [createError, setCreateError] = React.useState<string | null>(null);
  const [disableError, setDisableError] = React.useState<string | null>(null);

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
    () =>
      [...tenantsQuery.data].sort((left, right) =>
        right.createdAt.localeCompare(left.createdAt),
      ),
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
    handleDisableTenant(tenant).catch(() => undefined);
  };

  const createErrorAlert = createError ? (
    <Alert severity="error" sx={{ mt: 2 }}>
      {createError}
    </Alert>
  ) : null;

  const disableErrorAlert = disableError ? (
    <Alert severity="error">{disableError}</Alert>
  ) : null;

  const noTenantsMessage =
    sortedTenants.length === 0 ? (
      <Box sx={{ px: 3, py: 4 }}>
        <Typography color="text.secondary">No tenants created yet.</Typography>
      </Box>
    ) : null;

  return (
    <Stack spacing={3}>
      <Titlebar title="Tenant Admin" />

      <Paper sx={{ p: 3 }}>
        <Stack
          component="form"
          direction={{ xs: "column", md: "row" }}
          spacing={2}
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
      </Paper>

      <Paper sx={{ p: 0 }}>
        <Table size="small">
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
                      disabled={!canDisable || disableTenantMutation.isPending}
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
        {noTenantsMessage}
      </Paper>

      {disableErrorAlert}
    </Stack>
  );
};

export default TenantsAdminPage;
