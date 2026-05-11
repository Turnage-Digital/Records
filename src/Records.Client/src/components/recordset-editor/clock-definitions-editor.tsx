import * as React from "react";

import { AddCircle, Edit, PauseCircleOutline } from "@mui/icons-material";
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
} from "@mui/material";

import type { ClockDefinitionFormValue } from "./recordset-editor.types";
import type { ClockThresholdUnit } from "../../models/clock-threshold-unit";

interface ClockDefinitionsEditorProps {
  tenantId?: string | null;
  definitions: ClockDefinitionFormValue[];
  onAddDefinition: (definition: ClockDefinitionFormValue) => void;
  onUpdateDefinition: (definition: ClockDefinitionFormValue) => void;
  onRemoveDefinition: (definition: ClockDefinitionFormValue) => void;
}

const thresholdUnits: ClockThresholdUnit[] = [
  "Minutes",
  "Hours",
  "Days",
  "BusinessDays",
];

const thresholdUnitLabel: Record<ClockThresholdUnit, string> = {
  Minutes: "Minutes",
  Hours: "Hours",
  Days: "Days",
  BusinessDays: "Business Days",
};

const toThresholdLabel = (value: number, unit: ClockThresholdUnit): string =>
  `${value} ${thresholdUnitLabel[unit]}`;

const createClientId = () => {
  if (
    typeof crypto !== "undefined" &&
    typeof crypto.randomUUID === "function"
  ) {
    return crypto.randomUUID();
  }

  return `${Date.now()}`;
};

const ClockDefinitionsEditor = ({
  tenantId,
  definitions,
  onAddDefinition,
  onUpdateDefinition,
  onRemoveDefinition,
}: ClockDefinitionsEditorProps) => {
  const [dialogOpen, setDialogOpen] = React.useState(false);
  const [dialogMode, setDialogMode] = React.useState<"create" | "edit">(
    "create",
  );
  const [workingDefinition, setWorkingDefinition] =
    React.useState<ClockDefinitionFormValue | null>(null);
  const [formError, setFormError] = React.useState<string | null>(null);

  const openCreateDialog = () => {
    setDialogMode("create");
    setWorkingDefinition({
      tenantId: tenantId ?? undefined,
      name: "",
      atRiskThresholdValue: 4,
      atRiskThresholdUnit: "Hours",
      breachThresholdValue: 8,
      breachThresholdUnit: "Hours",
      isActive: true,
      clientId: createClientId(),
    });
    setFormError(null);
    setDialogOpen(true);
  };

  const openEditDialog = (definition: ClockDefinitionFormValue) => {
    setDialogMode("edit");
    setWorkingDefinition({ ...definition });
    setFormError(null);
    setDialogOpen(true);
  };

  const closeDialog = () => {
    setDialogOpen(false);
    setWorkingDefinition(null);
    setFormError(null);
  };

  const handleSubmit = () => {
    if (!workingDefinition) {
      return;
    }

    const trimmedName = workingDefinition.name.trim();
    if (trimmedName.length === 0) {
      setFormError("Name is required.");
      return;
    }

    if (
      workingDefinition.atRiskThresholdValue <= 0 ||
      workingDefinition.breachThresholdValue <= 0
    ) {
      setFormError("Threshold values must be greater than zero.");
      return;
    }

    const nextDefinition: ClockDefinitionFormValue = {
      ...workingDefinition,
      name: trimmedName,
    };

    if (dialogMode === "create") {
      onAddDefinition(nextDefinition);
    } else {
      onUpdateDefinition(nextDefinition);
    }

    closeDialog();
  };

  const tableRowsNode =
    definitions.length === 0 ? (
      <TableRow>
        <TableCell colSpan={5}>
          <Box sx={{ py: 4, textAlign: "center" }}>
            No clock definitions configured in this form.
          </Box>
        </TableCell>
      </TableRow>
    ) : (
      definitions.map((definition) => {
        const statusLabel = definition.isActive ? "Active" : "Disabled";
        const statusColor = definition.isActive ? "success" : "default";

        return (
          <TableRow key={definition.clientId} hover>
            <TableCell>{definition.name}</TableCell>
            <TableCell>
              {toThresholdLabel(
                definition.atRiskThresholdValue,
                definition.atRiskThresholdUnit,
              )}
            </TableCell>
            <TableCell>
              {toThresholdLabel(
                definition.breachThresholdValue,
                definition.breachThresholdUnit,
              )}
            </TableCell>
            <TableCell>
              <Chip size="small" label={statusLabel} color={statusColor} />
            </TableCell>
            <TableCell align="right">
              <Stack direction="row" spacing={1} justifyContent="flex-end">
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<Edit fontSize="small" />}
                  onClick={() => openEditDialog(definition)}
                >
                  Edit
                </Button>
                <Button
                  size="small"
                  variant="outlined"
                  color="warning"
                  startIcon={<PauseCircleOutline fontSize="small" />}
                  onClick={() => onRemoveDefinition(definition)}
                >
                  Disable
                </Button>
              </Stack>
            </TableCell>
          </TableRow>
        );
      })
    );

  const formErrorAlert = formError ? (
    <Alert severity="error">{formError}</Alert>
  ) : null;
  const dialogTitle =
    dialogMode === "create"
      ? "Create clock definition"
      : "Update clock definition";
  const submitLabel = dialogMode === "create" ? "Create" : "Save";

  return (
    <>
      <Stack spacing={2}>
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: { xs: "flex-start", sm: "center" },
            gap: 2,
            flexWrap: "wrap",
          }}
        >
          <Button
            variant="outlined"
            startIcon={<AddCircle />}
            onClick={openCreateDialog}
          >
            Create Clock Definition
          </Button>
        </Box>

        <Paper variant="outlined" sx={{ overflowX: "auto" }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>At risk threshold</TableCell>
                <TableCell>Breach threshold</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>{tableRowsNode}</TableBody>
          </Table>
        </Paper>
      </Stack>

      <Dialog open={dialogOpen} onClose={closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>{dialogTitle}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {formErrorAlert}
            <TextField
              label="Name"
              value={workingDefinition?.name ?? ""}
              onChange={(event) => {
                const nextName = event.target.value;
                setWorkingDefinition((current) =>
                  current ? { ...current, name: nextName } : current,
                );
              }}
              fullWidth
            />
            <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
              <TextField
                label="At risk value"
                type="number"
                value={workingDefinition?.atRiskThresholdValue ?? 0}
                onChange={(event) =>
                  setWorkingDefinition((current) =>
                    current
                      ? {
                          ...current,
                          atRiskThresholdValue: Number(event.target.value),
                        }
                      : current,
                  )
                }
                inputProps={{ min: 1 }}
                fullWidth
              />
              <FormControl fullWidth>
                <InputLabel id="clock-at-risk-unit-label">
                  At risk unit
                </InputLabel>
                <Select
                  labelId="clock-at-risk-unit-label"
                  label="At risk unit"
                  value={workingDefinition?.atRiskThresholdUnit ?? "Hours"}
                  onChange={(event) =>
                    setWorkingDefinition((current) =>
                      current
                        ? {
                            ...current,
                            atRiskThresholdUnit: event.target
                              .value as ClockThresholdUnit,
                          }
                        : current,
                    )
                  }
                >
                  {thresholdUnits.map((unit) => (
                    <MenuItem key={unit} value={unit}>
                      {thresholdUnitLabel[unit]}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
              <TextField
                label="Breach value"
                type="number"
                value={workingDefinition?.breachThresholdValue ?? 0}
                onChange={(event) =>
                  setWorkingDefinition((current) =>
                    current
                      ? {
                          ...current,
                          breachThresholdValue: Number(event.target.value),
                        }
                      : current,
                  )
                }
                inputProps={{ min: 1 }}
                fullWidth
              />
              <FormControl fullWidth>
                <InputLabel id="clock-breach-unit-label">
                  Breach unit
                </InputLabel>
                <Select
                  labelId="clock-breach-unit-label"
                  label="Breach unit"
                  value={workingDefinition?.breachThresholdUnit ?? "Hours"}
                  onChange={(event) =>
                    setWorkingDefinition((current) =>
                      current
                        ? {
                            ...current,
                            breachThresholdUnit: event.target
                              .value as ClockThresholdUnit,
                          }
                        : current,
                    )
                  }
                >
                  {thresholdUnits.map((unit) => (
                    <MenuItem key={unit} value={unit}>
                      {thresholdUnitLabel[unit]}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDialog}>Cancel</Button>
          <Button onClick={handleSubmit} variant="contained">
            {submitLabel}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ClockDefinitionsEditor;
