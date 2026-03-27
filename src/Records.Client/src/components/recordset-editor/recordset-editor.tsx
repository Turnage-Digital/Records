import * as React from "react";

import { PlayArrow, Save, Undo } from "@mui/icons-material";
import {
  Alert,
  AlertTitle,
  Box,
  Button,
  Divider,
  LinearProgress,
  Stack,
  Typography,
} from "@mui/material";

import {
  Column,
  MigrationJobStage,
  MigrationPlan,
  RecordsetItemDefinition,
  Status,
  StatusTransition,
} from "../../models";
import EditRecordsetColumnsContent from "../edit-recordset-columns-content";
import EditRecordsetNameContent from "../edit-recordset-name-content";
import EditRecordsetStatusesContent from "../edit-recordset-statuses-content";
import FormBlock from "../form-block";
import {
  type RecordsetEditorInitialValue,
  type RecordsetEditorSubmitResult,
  RecordsetMigrationRequiredError,
} from "./recordset-editor.types";
import StatusTransitionsEditor from "./status-transitions-editor";

interface RecordsetEditorProps {
  initialValue: RecordsetEditorInitialValue;
  onSubmit: (result: RecordsetEditorSubmitResult) => Promise<void> | void;
  isSubmitting?: boolean;
  isMigrationPending?: boolean;
  onRequestMigration?: (plan: MigrationPlan) => Promise<void> | void;
  migrationStatus?: MigrationStatusBanner | null;
  onCancel?: () => void;
  disableNameField?: boolean;
  onStatusesChanged?: (statuses: Status[]) => void;
}

interface MigrationStatusBanner {
  stage: MigrationJobStage;
  message: string;
  percent?: number;
}

interface InternalState {
  id?: string | null;
  name: string;
  columns: Column[];
  statuses: Status[];
  transitions: StatusTransition[];
}

const sanitizeTransitions = (
  statuses: Status[],
  transitions?: StatusTransition[] | null,
): StatusTransition[] => {
  const allowedNames = new Set(statuses.map((status) => status.name));
  const safeTransitions = Array.isArray(transitions) ? transitions : [];
  return safeTransitions
    .filter(
      (transition): transition is StatusTransition =>
        Boolean(transition.from) && allowedNames.has(String(transition.from)),
    )
    .map((transition) => {
      const allowedNext = Array.isArray(transition.allowedNext)
        ? transition.allowedNext
        : [];
      return {
        from: transition.from,
        allowedNext: allowedNext.filter((name) => allowedNames.has(name)),
      };
    })
    .filter((transition) => transition.allowedNext.length > 0);
};

const RecordsetEditor = ({
  initialValue,
  onSubmit,
  isSubmitting,
  isMigrationPending,
  onRequestMigration,
  migrationStatus,
  onCancel,
  disableNameField = false,
  onStatusesChanged,
}: RecordsetEditorProps) => {
  const [state, setState] = React.useState<InternalState>(() => ({
    id: initialValue.id ?? null,
    name: initialValue.name,
    columns: [...initialValue.columns],
    statuses: [...initialValue.statuses],
    transitions: sanitizeTransitions(
      initialValue.statuses,
      initialValue.transitions,
    ),
  }));

  const [error, setError] = React.useState<string | null>(null);
  const [serverFeedback, setServerFeedback] = React.useState<{
    message: string;
    reasons?: string[];
    plan?: MigrationPlan;
  } | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedName = state.name.trim();
    if (!trimmedName) {
      setServerFeedback(null);
      setError("Recordset name is required.");
      return;
    }

    const sanitizedTransitions = sanitizeTransitions(
      state.statuses,
      state.transitions,
    );

    const definition: RecordsetItemDefinition = {
      id: state.id ?? null,
      name: trimmedName,
      columns: state.columns,
      statuses: state.statuses,
      transitions: sanitizedTransitions,
    };

    const result: RecordsetEditorSubmitResult = {
      definition,
    };

    setError(null);
    setServerFeedback(null);
    try {
      await onSubmit(result);
    } catch (err) {
      if (err instanceof RecordsetMigrationRequiredError) {
        setServerFeedback({
          message: err.message,
          reasons: err.reasons,
          plan: err.plan,
        });
        return;
      }

      if (err instanceof Error) {
        setServerFeedback({ message: err.message });
      } else {
        setServerFeedback({ message: "Failed to update recordset." });
      }
    }
  };

  const handleNameChange = (name: string) => {
    setState((prev) => ({ ...prev, name }));
  };

  const handleColumnsChange = (columns: Column[]) => {
    setState((prev) => ({ ...prev, columns }));
  };

  const handleStatusesChange = (statuses: Status[]) => {
    setState((prev) => ({
      ...prev,
      statuses,
      transitions: sanitizeTransitions(statuses, prev.transitions),
    }));
  };

  const handleTransitionsChange = (transitions: StatusTransition[]) => {
    setState((prev) => ({ ...prev, transitions }));
  };

  React.useEffect(() => {
    onStatusesChanged?.(state.statuses);
  }, [onStatusesChanged, state.statuses]);

  const errorBanner = error ? (
    <Alert severity="error" sx={{ maxWidth: 720 }}>
      {error}
    </Alert>
  ) : null;

  const serverFeedbackPlan = serverFeedback?.plan ?? null;
  const serverFeedbackReasons = serverFeedback?.reasons ?? [];
  const canRequestMigration = Boolean(serverFeedbackPlan && onRequestMigration);

  const handleRunMigrationClick = async () => {
    if (!onRequestMigration || !serverFeedbackPlan) {
      return;
    }

    try {
      await onRequestMigration(serverFeedbackPlan);
      setServerFeedback(null);
    } catch (caughtError) {
      if (caughtError instanceof Error) {
        setServerFeedback({
          message: caughtError.message,
          reasons: serverFeedbackReasons,
          plan: serverFeedbackPlan,
        });
      }
    }
  };

  let serverFeedbackReasonsContent: React.ReactNode = null;
  if (serverFeedbackReasons.length > 0) {
    serverFeedbackReasonsContent = (
      <Box component="ul" sx={{ pl: 3, mb: 0 }}>
        {serverFeedbackReasons.map((reason) => (
          <Box component="li" key={reason} sx={{ mt: 0.5 }}>
            {reason}
          </Box>
        ))}
      </Box>
    );
  }

  let serverFeedbackActionContent: React.ReactNode = null;
  if (canRequestMigration) {
    serverFeedbackActionContent = (
      <Box sx={{ mt: 2 }}>
        <Button
          variant="contained"
          size="small"
          startIcon={<PlayArrow />}
          onClick={handleRunMigrationClick}
          disabled={Boolean(isMigrationPending)}
        >
          Run migration
        </Button>
      </Box>
    );
  }

  const serverFeedbackBanner = serverFeedback ? (
    <Alert severity="warning" sx={{ maxWidth: 720 }}>
      <AlertTitle>{serverFeedback.message}</AlertTitle>
      {serverFeedbackReasonsContent}
      {serverFeedbackActionContent}
    </Alert>
  ) : null;

  let migrationSeverity: "error" | "info" | "success" | "warning" = "info";
  if (migrationStatus?.stage === "Failed") {
    migrationSeverity = "error";
  } else if (
    migrationStatus?.stage === "Completed" ||
    migrationStatus?.stage === "Archived"
  ) {
    migrationSeverity = "success";
  }

  const migrationPercentValue =
    typeof migrationStatus?.percent === "number"
      ? Math.max(0, Math.min(100, migrationStatus.percent))
      : null;

  let migrationProgressContent: React.ReactNode = null;
  if (typeof migrationPercentValue === "number") {
    migrationProgressContent = (
      <Box sx={{ mt: 2 }}>
        <LinearProgress variant="determinate" value={migrationPercentValue} />
        <Typography
          component="div"
          variant="caption"
          sx={{ display: "block", textAlign: "right", mt: 0.5 }}
        >
          {Math.round(migrationPercentValue)}%
        </Typography>
      </Box>
    );
  }

  const migrationMessageBanner = migrationStatus ? (
    <Alert severity={migrationSeverity} sx={{ maxWidth: 720 }}>
      <AlertTitle>{migrationStatus.stage}</AlertTitle>
      <Typography component="div" variant="body2">
        {migrationStatus.message}
      </Typography>
      {migrationProgressContent}
    </Alert>
  ) : null;

  return (
    <Stack
      component="form"
      onSubmit={handleSubmit}
      divider={<Divider sx={{ my: { xs: 5, md: 6 } }} />}
      spacing={{ xs: 6, md: 7 }}
    >
      {errorBanner}
      {serverFeedbackBanner}
      {migrationMessageBanner}

      <FormBlock
        title="Name"
        subtitle="Give this recordset a clear, human-friendly name so everyone knows what lives here."
        content={
          <EditRecordsetNameContent
            name={state.name}
            onNameChanged={handleNameChange}
            disabled={disableNameField}
          />
        }
      />

      <FormBlock
        title="Columns"
        subtitle="Define the fields each record should capture — text, numbers, dates, or yes/no toggles."
        content={
          <EditRecordsetColumnsContent
            columns={state.columns}
            onColumnsChanged={handleColumnsChange}
          />
        }
      />

      <FormBlock
        title="Statuses"
        subtitle="Define the steps in your workflow so records move from start to finish in a predictable way."
        content={
          <EditRecordsetStatusesContent
            statuses={state.statuses}
            onStatusesChanged={handleStatusesChange}
          />
        }
      />

      <FormBlock
        title="Status transitions"
        subtitle="Choose which status changes are allowed to keep records flowing through the process safely."
        content={
          <StatusTransitionsEditor
            statuses={state.statuses}
            transitions={state.transitions}
            onChange={handleTransitionsChange}
          />
        }
      />

      <Box
        sx={{
          display: "flex",
          flexWrap: "wrap",
          gap: 2,
          justifyContent: { xs: "center", md: "flex-end" },
          alignItems: "center",
        }}
      >
        {errorBanner}
        {onCancel && (
          <Button
            type="button"
            onClick={onCancel}
            startIcon={<Undo />}
            variant="text"
          >
            Cancel
          </Button>
        )}
        <Button
          type="submit"
          variant="contained"
          startIcon={<Save />}
          disabled={isSubmitting || Boolean(isMigrationPending)}
        >
          Submit
        </Button>
      </Box>
    </Stack>
  );
};

export default RecordsetEditor;
