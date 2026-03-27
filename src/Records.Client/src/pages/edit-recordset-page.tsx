import * as React from "react";

import { Box, Button, Stack, Typography } from "@mui/material";
import {
  useMutation,
  useQuery,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import { useAuth } from "../auth";
import {
  type ClockDefinitionFormValue,
  type ClockDefinitionSubmission,
  Loading,
  type NotificationRuleFormValue,
  type NotificationRuleSubmission,
  RecordsetEditor,
  type RecordsetEditorInitialValue,
  type RecordsetEditorSubmitResult,
  RecordsetMigrationRequiredError,
  Titlebar,
  toClockDefinitionFormValue,
  toNotificationRuleFormValue,
  useSideDrawer,
} from "../components";
import { resolveActorUlid, resolveTenantUlid } from "../lib/identifiers";
import {
  clockDefinitionsQueryOptions,
  migrationProgressQueryOptions,
  notificationRulesQueryOptions,
  recordsetItemDefinitionQueryOptions,
  tenantSummariesQueryOptions,
} from "../query-options";

import type { MigrationPlan, MigrationProgressRecord, Status } from "../models";

const ClockDefinitionsDrawer = React.lazy(
  () => import("../components/recordset-editor/clock-definitions-drawer"),
);
const NotificationRulesDrawer = React.lazy(
  () => import("../components/recordset-editor/notification-rules-drawer"),
);

type NotificationRuleMutationInput = Pick<
  NotificationRuleSubmission,
  "trigger" | "channels" | "schedule" | "isActive"
>;

const buildNotificationRulePayload = (
  input: NotificationRuleMutationInput,
) => ({
  trigger: input.trigger,
  channels: input.channels,
  schedule: input.schedule,
  isActive: input.isActive,
});

const createNotificationRule = async (
  recordsetId: string,
  tenantId: string,
  input: NotificationRuleMutationInput,
) => {
  const payload = {
    recordsetId,
    tenantId,
    ...buildNotificationRulePayload(input),
  };

  const response = await fetch("/api/notifications/rules", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to create notification rule");
    throw new Error(message);
  }

  await response.json();
};

const updateNotificationRule = async (
  ruleId: string,
  input: NotificationRuleMutationInput,
) => {
  const response = await fetch(`/api/notifications/rules/${ruleId}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(buildNotificationRulePayload(input)),
  });

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to update notification rule");
    throw new Error(message);
  }
};

const deleteNotificationRule = async (ruleId: string) => {
  const response = await fetch(`/api/notifications/rules/${ruleId}`, {
    method: "DELETE",
  });

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to delete notification rule");
    throw new Error(message);
  }
};

const getPreferredTenantId = (
  preferredTenantId: string | undefined,
  tenantIds: string[],
): string | null => {
  if (preferredTenantId && tenantIds.includes(preferredTenantId)) {
    return preferredTenantId;
  }

  if (tenantIds.length > 0) {
    return tenantIds[0];
  }

  return null;
};

const createClockDefinition = async (
  tenantId: string,
  input: ClockDefinitionSubmission,
) => {
  const response = await fetch("/api/clock-definitions", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      tenantId,
      name: input.name,
      atRiskThresholdValue: input.atRiskThresholdValue,
      atRiskThresholdUnit: input.atRiskThresholdUnit,
      breachThresholdValue: input.breachThresholdValue,
      breachThresholdUnit: input.breachThresholdUnit,
    }),
  });

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to create clock definition");
    throw new Error(message);
  }
};

const updateClockDefinition = async (
  definitionId: string,
  input: ClockDefinitionSubmission,
) => {
  const response = await fetch(`/api/clock-definitions/${definitionId}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      definitionId,
      name: input.name,
      atRiskThresholdValue: input.atRiskThresholdValue,
      atRiskThresholdUnit: input.atRiskThresholdUnit,
      breachThresholdValue: input.breachThresholdValue,
      breachThresholdUnit: input.breachThresholdUnit,
    }),
  });

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to update clock definition");
    throw new Error(message);
  }
};

const disableClockDefinition = async (definitionId: string) => {
  const response = await fetch(
    `/api/clock-definitions/${definitionId}/disable`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        definitionId,
      }),
    },
  );

  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to disable clock definition");
    throw new Error(message);
  }
};

const EditRecordsetPage = () => {
  const auth = useAuth();
  const { openDrawer } = useSideDrawer();
  const { recordsetId } = useParams<{ recordsetId: string }>();
  if (!recordsetId) {
    throw new Error("Recordset id is required");
  }

  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeMigration, setActiveMigration] = React.useState<{
    correlationId: string;
  } | null>(null);

  const migrationProgressQuery = useQuery(
    migrationProgressQueryOptions(
      recordsetId,
      activeMigration?.correlationId ?? "",
    ),
  );
  const migrationProgress = migrationProgressQuery.data ?? null;

  const migrationStatus = React.useMemo(() => {
    if (!migrationProgress) {
      return null;
    }

    const stage = migrationProgress.stage;
    let percent = migrationProgress.percent;
    if (typeof percent !== "number") {
      if (stage === "Completed" || stage === "Archived") {
        percent = 100;
      } else if (stage === "Failed") {
        percent = 0;
      }
    }

    let message = migrationProgress.lastMessage;
    if (stage === "Completed") {
      if (typeof migrationProgress.itemsProcessed === "number") {
        message = `Migration completed — ${migrationProgress.itemsProcessed} record${
          migrationProgress.itemsProcessed === 1 ? "" : "s"
        } processed.`;
      } else {
        message = message ?? "Migration completed.";
      }
    } else if (stage === "Failed") {
      message = migrationProgress.lastError ?? message ?? "Migration failed.";
    } else if (stage === "Archived") {
      message = message ?? "Backup removed after retention window.";
    }

    if (!message) {
      if (stage === "Running") {
        message = "Migration in progress.";
      } else if (stage === "Pending") {
        message = "Migration queued.";
      } else {
        message = `Migration ${stage.toLowerCase()}.`;
      }
    }

    return {
      stage,
      message,
      percent,
    };
  }, [migrationProgress]);

  const recordsetDefinitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );

  const notificationRulesQuery = useSuspenseQuery(
    notificationRulesQueryOptions(recordsetId),
  );
  const tenantsQuery = useSuspenseQuery(tenantSummariesQueryOptions());
  const tenantIds = tenantsQuery.data.map((tenant) => tenant.tenantId);
  const preferredTenantId = resolveTenantUlid(auth.user);
  const selectedTenantId = React.useMemo(
    () => getPreferredTenantId(preferredTenantId, tenantIds),
    [preferredTenantId, tenantIds],
  );
  const clockDefinitionsQuery = useQuery(
    clockDefinitionsQueryOptions(selectedTenantId ?? undefined),
  );

  const [notificationRules, setNotificationRules] = React.useState<
    NotificationRuleFormValue[]
  >([]);
  const [deletedNotificationRuleIds, setDeletedNotificationRuleIds] =
    React.useState<string[]>([]);
  const [clockDefinitions, setClockDefinitions] = React.useState<
    ClockDefinitionFormValue[]
  >([]);
  const [disabledClockDefinitionIds, setDisabledClockDefinitionIds] =
    React.useState<string[]>([]);
  const [draftStatuses, setDraftStatuses] = React.useState<Status[]>([]);
  const initializedNotificationRulesRef = React.useRef<string | null>(null);
  const initializedClockTenantRef = React.useRef<string | null>(null);
  const initializedStatusesRef = React.useRef(false);

  React.useEffect(() => {
    if (initializedNotificationRulesRef.current === recordsetId) {
      return;
    }

    initializedNotificationRulesRef.current = recordsetId;
    const nextRules = notificationRulesQuery.data.map(
      toNotificationRuleFormValue,
    );
    setNotificationRules(nextRules);
    setDeletedNotificationRuleIds([]);
  }, [notificationRulesQuery.data, recordsetId]);

  React.useEffect(() => {
    if (selectedTenantId === initializedClockTenantRef.current) {
      return;
    }

    if (selectedTenantId && typeof clockDefinitionsQuery.data === "undefined") {
      return;
    }

    initializedClockTenantRef.current = selectedTenantId;

    const nextDefinitions = (clockDefinitionsQuery.data ?? []).map(
      toClockDefinitionFormValue,
    );
    setClockDefinitions(nextDefinitions);
    setDisabledClockDefinitionIds([]);
  }, [clockDefinitionsQuery.data, selectedTenantId]);

  const definition = recordsetDefinitionQuery.data;

  React.useEffect(() => {
    if (initializedStatusesRef.current) {
      return;
    }

    initializedStatusesRef.current = true;
    setDraftStatuses(definition.statuses);
  }, [definition.statuses]);

  const initialValue = React.useMemo<RecordsetEditorInitialValue>(() => {
    const definition = recordsetDefinitionQuery.data;
    const transitions = Array.isArray(definition.transitions)
      ? definition.transitions
      : [];
    const definitionId = definition.id ?? recordsetId;

    return {
      id: definitionId,
      name: definition.name,
      columns: definition.columns,
      statuses: definition.statuses,
      transitions,
    };
  }, [recordsetDefinitionQuery.data, recordsetId]);

  const persistNotificationRules = async (
    rules: NotificationRuleFormValue[],
    deletedRuleIds: string[],
  ) => {
    const upserts = rules.filter((rule) => rule.channels.length > 0);
    const creates = upserts.filter((rule) => !rule.id);
    const updates = upserts.filter(
      (rule): rule is NotificationRuleFormValue & { id: string } =>
        typeof rule.id === "string" && rule.id.length > 0,
    );

    if (creates.length > 0) {
      const tenantId = selectedTenantId;
      if (!tenantId) {
        throw new Error("Notification rules require a tenant context.");
      }

      await Promise.all(
        creates.map((rule) =>
          createNotificationRule(recordsetId, tenantId, {
            trigger: rule.trigger,
            channels: rule.channels,
            schedule: rule.schedule,
            isActive: rule.isActive,
          }),
        ),
      );
    }

    if (updates.length > 0) {
      await Promise.all(
        updates.map((rule) =>
          updateNotificationRule(rule.id, {
            trigger: rule.trigger,
            channels: rule.channels,
            schedule: rule.schedule,
            isActive: rule.isActive,
          }),
        ),
      );
    }

    if (deletedRuleIds.length > 0) {
      await Promise.all(
        deletedRuleIds.map((ruleId) => deleteNotificationRule(ruleId)),
      );
    }

    const refreshedRules = await queryClient.fetchQuery(
      notificationRulesQueryOptions(recordsetId),
    );
    setNotificationRules(refreshedRules.map(toNotificationRuleFormValue));
    setDeletedNotificationRuleIds([]);
  };

  const persistClockDefinitions = async (
    definitions: ClockDefinitionFormValue[],
    disabledDefinitionIds: string[],
  ) => {
    const actorId = resolveActorUlid(auth.user);
    const clockUpserts = definitions
      .map((definition) => ({
        id: definition.id,
        tenantId: definition.tenantId,
        name: definition.name,
        atRiskThresholdValue: definition.atRiskThresholdValue,
        atRiskThresholdUnit: definition.atRiskThresholdUnit,
        breachThresholdValue: definition.breachThresholdValue,
        breachThresholdUnit: definition.breachThresholdUnit,
        isActive: definition.isActive,
      }))
      .map((definition) => ({
        ...definition,
        name: definition.name.trim(),
      }))
      .filter((definition) => definition.name.length > 0);

    const clockCreates = clockUpserts.filter(
      (definition) => !definition.id && definition.isActive,
    );
    const clockUpdates = clockUpserts.filter(
      (
        definition,
      ): definition is (typeof clockUpserts)[number] & { id: string } =>
        typeof definition.id === "string" &&
        definition.id.length > 0 &&
        definition.isActive,
    );

    if (clockCreates.length > 0) {
      const tenantId = selectedTenantId;
      if (!tenantId) {
        throw new Error("Clock definitions require a tenant context.");
      }

      await Promise.all(
        clockCreates.map((definition) =>
          createClockDefinition(tenantId, definition),
        ),
      );
    }

    if (clockUpdates.length > 0) {
      await Promise.all(
        clockUpdates.map((definition) =>
          updateClockDefinition(definition.id, definition),
        ),
      );
    }

    if (disabledDefinitionIds.length > 0) {
      await Promise.all(
        disabledDefinitionIds.map((definitionId) =>
          disableClockDefinition(definitionId),
        ),
      );
    }

    if (!selectedTenantId) {
      setClockDefinitions([]);
      setDisabledClockDefinitionIds([]);
      return;
    }

    const refreshedDefinitions = await queryClient.fetchQuery(
      clockDefinitionsQueryOptions(selectedTenantId),
    );
    setClockDefinitions(refreshedDefinitions.map(toClockDefinitionFormValue));
    setDisabledClockDefinitionIds([]);
  };

  const updateRecordsetMutation = useMutation({
    mutationFn: async (result: RecordsetEditorSubmitResult) => {
      const { definition } = result;
      const updatePayload = {
        recordsetId,
        columns: definition.columns,
        statuses: definition.statuses,
        statusTransitions: definition.transitions,
      };

      const response = await fetch(`/api/recordsets/${recordsetId}/schema`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(updatePayload),
      });

      if (!response.ok) {
        const clone = response.clone();
        const contentType = response.headers.get("content-type") ?? "";

        let parsedBody: unknown = null;
        if (contentType.includes("application/json")) {
          parsedBody = await response.json().catch(() => null);
        }

        let message: string | undefined;
        let reasons: string[] | undefined;
        let plan: MigrationPlan | undefined;

        if (parsedBody && typeof parsedBody === "object") {
          const bodyRecord = parsedBody as Record<string, unknown>;
          if (
            typeof bodyRecord.message === "string" &&
            bodyRecord.message.length > 0
          ) {
            message = bodyRecord.message;
          }

          if (Array.isArray(bodyRecord.reasons)) {
            const filtered = bodyRecord.reasons.filter(
              (reason): reason is string =>
                typeof reason === "string" && reason.trim().length > 0,
            );
            if (filtered.length > 0) {
              reasons = filtered;
            }
          }

          if (bodyRecord.plan && typeof bodyRecord.plan === "object") {
            plan = bodyRecord.plan as MigrationPlan;
          }
        }

        if (!message) {
          const text = await clone.text().catch(() => "");
          if (text && text.length > 0) {
            message = text;
          }
        }

        if (response.status === 409 && reasons && reasons.length > 0) {
          throw new RecordsetMigrationRequiredError(
            message ?? "This update requires a recordset migration.",
            reasons,
            plan,
          );
        }

        throw new Error(
          message || "Failed to update recordset. Check for validation errors.",
        );
      }
    },
    onSuccess: async () => {
      setActiveMigration(null);
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["recordset-definition", recordsetId],
        }),
        queryClient.invalidateQueries({
          queryKey: ["recordset-names"],
        }),
      ]);
      handleNavigateToRecordset();
    },
  });

  interface MigrationRequestResponse {
    messages?: string[];
    correlationId?: string;
  }

  const requestMigrationMutation = useMutation<
    MigrationRequestResponse,
    Error,
    MigrationPlan
  >({
    mutationFn: async (plan: MigrationPlan) => {
      const response = await fetch(
        `/api/recordsets/${recordsetId}/migrations`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ plan, mode: "execute" }),
        },
      );

      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to queue migration.");
        throw new Error(message);
      }

      return (await response.json()) as MigrationRequestResponse;
    },
    onSuccess: async (result) => {
      const messages = Array.isArray(result.messages) ? result.messages : [];
      const message =
        messages.length > 0 ? messages[0] : "Migration queued successfully.";
      const correlationId =
        typeof result.correlationId === "string"
          ? result.correlationId
          : undefined;

      if (correlationId) {
        setActiveMigration({ correlationId });
        queryClient.setQueryData<MigrationProgressRecord | null>(
          ["recordset-migration-progress", recordsetId, correlationId],
          (previous) => {
            const base: MigrationProgressRecord = previous ?? {
              recordsetId,
              correlationId,
              stage: "Pending",
              createdOn: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
            };

            return {
              ...base,
              stage: "Pending",
              lastMessage: message,
              percent: 0,
              updatedAt: new Date().toISOString(),
            };
          },
        );
      }

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["recordset-definition", recordsetId],
        }),
        queryClient.invalidateQueries({
          queryKey: ["notification-rules", recordsetId],
        }),
      ]);
    },
    onError: () => {
      setActiveMigration(null);
    },
  });

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const handleNavigateToRecordset = () => {
    navigate(`/${recordsetId}?page=0&pageSize=10`);
  };

  const handleSubmit = async (result: RecordsetEditorSubmitResult) => {
    setActiveMigration(null);
    await updateRecordsetMutation.mutateAsync(result);
  };

  const handleOpenNotificationRulesDrawer = () => {
    openDrawer(
      "Notification Rules",
      <NotificationRulesDrawer
        initialRules={notificationRules}
        initialDeletedRuleIds={deletedNotificationRuleIds}
        statuses={draftStatuses}
        onSave={persistNotificationRules}
      />,
    );
  };

  const handleOpenClockDefinitionsDrawer = () => {
    openDrawer(
      "Clock Definitions",
      <ClockDefinitionsDrawer
        tenantId={selectedTenantId}
        initialDefinitions={clockDefinitions}
        initialDisabledDefinitionIds={disabledClockDefinitionIds}
        onSave={persistClockDefinitions}
      />,
    );
  };

  const handleRequestMigration = async (plan: MigrationPlan) => {
    await requestMigrationMutation.mutateAsync(plan);
  };

  const handleCancel = () => {
    navigate(-1);
  };

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
    {
      title: definition.name,
      onClick: handleNavigateToRecordset,
    },
  ];

  if (selectedTenantId && clockDefinitionsQuery.isPending) {
    return (
      <>
        <Titlebar title={`Edit ${definition.name}`} breadcrumbs={breadcrumbs} />
        <Loading />
      </>
    );
  }

  return (
    <>
      <Titlebar title={`Edit ${definition.name}`} breadcrumbs={breadcrumbs} />
      <Box sx={{ px: 3, pt: 1, pb: 2 }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
          <Button
            variant="outlined"
            onClick={handleOpenNotificationRulesDrawer}
            disabled={updateRecordsetMutation.isPending}
          >
            Notification Rules ({notificationRules.length})
          </Button>
          <Button
            variant="outlined"
            onClick={handleOpenClockDefinitionsDrawer}
            disabled={updateRecordsetMutation.isPending}
          >
            Clock Definitions ({clockDefinitions.length})
          </Button>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
          Configure rules and clocks in drawers, then submit schema changes
          below.
        </Typography>
      </Box>
      <RecordsetEditor
        key={`${recordsetId}:${selectedTenantId ?? "none"}`}
        disableNameField
        initialValue={initialValue}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isSubmitting={updateRecordsetMutation.isPending}
        isMigrationPending={requestMigrationMutation.isPending}
        migrationStatus={migrationStatus}
        onRequestMigration={handleRequestMigration}
        onStatusesChanged={setDraftStatuses}
      />
    </>
  );
};

export default EditRecordsetPage;
