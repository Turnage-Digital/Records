import * as React from "react";

import { Box, Button, Stack, Typography } from "@mui/material";
import {
  useMutation,
  useQuery,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { createSearchParams, useNavigate } from "react-router-dom";

import { useAuth } from "../auth";
import {
  Loading,
  RecordsetEditor,
  Titlebar,
  toClockDefinitionFormValue,
  useSideDrawer,
} from "../components";
import { extractRecordsetId, resolveTenantUlid } from "../lib/identifiers";
import {
  NotificationRuleInput,
  RecordsetItemDefinition,
  Status,
} from "../models";
import {
  clockDefinitionsQueryOptions,
  tenantSummariesQueryOptions,
} from "../query-options";

import type {
  ClockDefinitionFormValue,
  ClockDefinitionSubmission,
  NotificationRuleFormValue,
  RecordsetEditorInitialValue,
  RecordsetEditorSubmitResult,
} from "../components";

const ClockDefinitionsDrawer = React.lazy(
  () => import("../components/recordset-editor/clock-definitions-drawer"),
);
const NotificationRulesDrawer = React.lazy(
  () => import("../components/recordset-editor/notification-rules-drawer"),
);

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

const createNotificationRule = async (
  recordsetId: string,
  tenantId: string,
  input: NotificationRuleInput,
) => {
  const payload = {
    recordsetId,
    tenantId,
    trigger: input.trigger,
    channels: input.channels,
    schedule: input.schedule,
    isActive: input.isActive,
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

const CreateRecordsetPage = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { openDrawer } = useSideDrawer();

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
  const initializedClockTenantRef = React.useRef<string | null>(null);

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

  const initialValue = React.useMemo<RecordsetEditorInitialValue>(() => {
    return {
      id: null,
      name: "",
      columns: [],
      statuses: [],
      transitions: [],
    };
  }, []);

  const createRecordsetMutation = useMutation({
    mutationFn: async (result: RecordsetEditorSubmitResult) => {
      const { definition } = result;
      const request = new Request("/api/recordsets", {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify({
          name: definition.name,
          columns: definition.columns,
          statuses: definition.statuses,
          statusTransitions: definition.transitions,
        }),
      });
      const response = await fetch(request);
      if (!response.ok) {
        throw new Error("Failed to create recordset");
      }

      const created = (await response.json()) as unknown;
      const createdId = extractRecordsetId(created);
      if (!createdId) {
        throw new Error("Recordset was created without an identifier");
      }

      if (notificationRules.length > 0) {
        const tenantId = selectedTenantId;
        if (!tenantId) {
          throw new Error("Notification rules require a tenant context.");
        }

        await Promise.all(
          notificationRules
            .filter((rule) => rule.channels.length > 0)
            .map((rule) =>
              createNotificationRule(createdId, tenantId, {
                trigger: rule.trigger,
                channels: rule.channels,
                schedule: rule.schedule,
                isActive: rule.isActive,
              }),
            ),
        );
      }

      const clockUpserts = clockDefinitions
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

      if (disabledClockDefinitionIds.length > 0) {
        await Promise.all(
          disabledClockDefinitionIds.map((definitionId) =>
            disableClockDefinition(definitionId),
          ),
        );
      }

      return { id: createdId } satisfies Pick<RecordsetItemDefinition, "id">;
    },
    onSuccess: async (created) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["recordset-names"],
        }),
        queryClient.invalidateQueries({
          queryKey: ["clock-definitions"],
          exact: false,
        }),
      ]);
      const search = createSearchParams({
        page: "0",
        pageSize: "10",
      }).toString();
      navigate(`/${created.id}?${search}`);
    },
  });

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const handleSubmit = async (result: RecordsetEditorSubmitResult) => {
    await createRecordsetMutation.mutateAsync(result);
  };

  const handleOpenNotificationRulesDrawer = () => {
    openDrawer(
      "Notification Rules",
      <NotificationRulesDrawer
        initialRules={notificationRules}
        initialDeletedRuleIds={deletedNotificationRuleIds}
        statuses={draftStatuses}
        onSave={(rules, deletedRuleIds) => {
          setNotificationRules(rules);
          setDeletedNotificationRuleIds(deletedRuleIds);
        }}
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
        onSave={(definitions, disabledDefinitionIds) => {
          setClockDefinitions(definitions);
          setDisabledClockDefinitionIds(disabledDefinitionIds);
        }}
      />,
    );
  };

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
  ];

  if (selectedTenantId && clockDefinitionsQuery.isPending) {
    return (
      <>
        <Titlebar title="Create a Recordset" breadcrumbs={breadcrumbs} />
        <Loading />
      </>
    );
  }

  return (
    <>
      <Titlebar title="Create a Recordset" breadcrumbs={breadcrumbs} />
      <Box sx={{ px: 3, pt: 1, pb: 2 }}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
          <Button
            variant="outlined"
            onClick={handleOpenNotificationRulesDrawer}
            disabled={createRecordsetMutation.isPending}
          >
            Notification Rules ({notificationRules.length})
          </Button>
          <Button
            variant="outlined"
            onClick={handleOpenClockDefinitionsDrawer}
            disabled={createRecordsetMutation.isPending}
          >
            Clock Definitions ({clockDefinitions.length})
          </Button>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
          Configure rules and clocks in drawers, then submit the core recordset
          schema below.
        </Typography>
      </Box>
      <RecordsetEditor
        key={selectedTenantId ?? "none"}
        initialValue={initialValue}
        onSubmit={handleSubmit}
        isSubmitting={createRecordsetMutation.isPending}
        onStatusesChanged={setDraftStatuses}
      />
    </>
  );
};

export default CreateRecordsetPage;
