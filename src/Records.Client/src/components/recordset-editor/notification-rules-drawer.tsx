import * as React from "react";

import {
  Alert,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from "@mui/material";

import NotificationRulesEditor from "./notification-rules-editor";
import { createEmptyRuleFormValue } from "./notification-rules.helpers";
import DetailPanelContainer from "../detail-panel/detail-panel-container";
import DetailPanelContent from "../detail-panel/detail-panel-content";
import DetailPanelFooter from "../detail-panel/detail-panel-footer";
import DetailPanelHeader from "../detail-panel/detail-panel-header";
import useDetailPanel from "../detail-panel/use-detail-panel";

import type { NotificationRuleFormValue } from "./recordset-editor.types";
import type { Status } from "../../models/status";

interface NotificationRulesDrawerProps {
  initialRules: NotificationRuleFormValue[];
  initialDeletedRuleIds: string[];
  statuses: Status[];
  onSave: (
    rules: NotificationRuleFormValue[],
    deletedRuleIds: string[],
  ) => Promise<void> | void;
}

const cloneRule = (
  rule: NotificationRuleFormValue,
): NotificationRuleFormValue => ({
  ...rule,
  trigger: { ...rule.trigger },
  channels: rule.channels.map((channel) => ({
    ...channel,
    settings: channel.settings ? { ...channel.settings } : undefined,
  })),
  schedule: {
    ...rule.schedule,
    daysOfWeek: rule.schedule.daysOfWeek
      ? [...rule.schedule.daysOfWeek]
      : undefined,
  },
});

const NotificationRulesDrawer = ({
  initialRules,
  initialDeletedRuleIds,
  statuses,
  onSave,
}: NotificationRulesDrawerProps) => {
  const { closeDetailPanel } = useDetailPanel();
  const [rules, setRules] = React.useState<NotificationRuleFormValue[]>(() =>
    initialRules.map(cloneRule),
  );
  const [deletedRuleIds, setDeletedRuleIds] = React.useState<string[]>(
    initialDeletedRuleIds,
  );
  const [saveError, setSaveError] = React.useState<string | null>(null);
  const [isSaving, setIsSaving] = React.useState(false);

  const handleAddRule = () => {
    setRules((previous) => [...previous, createEmptyRuleFormValue()]);
  };

  const handleUpdateRule = (
    clientId: string,
    updater: (rule: NotificationRuleFormValue) => NotificationRuleFormValue,
  ) => {
    setRules((previous) =>
      previous.map((rule) =>
        rule.clientId === clientId ? updater(rule) : rule,
      ),
    );
  };

  const handleRemoveRule = (rule: NotificationRuleFormValue) => {
    setRules((previous) =>
      previous.filter((existing) => existing.clientId !== rule.clientId),
    );
    if (rule.id) {
      const ruleId = rule.id;
      setDeletedRuleIds((previous) =>
        previous.includes(ruleId) ? previous : [...previous, ruleId],
      );
    }
  };

  const handleSave = async () => {
    setSaveError(null);
    setIsSaving(true);
    try {
      await onSave(rules.map(cloneRule), deletedRuleIds);
      closeDetailPanel();
    } catch (error) {
      if (error instanceof Error && error.message.trim().length > 0) {
        setSaveError(error.message);
      } else {
        setSaveError("Failed to save notification rules.");
      }
    } finally {
      setIsSaving(false);
    }
  };
  const rulesLabelSuffix = rules.length === 1 ? "" : "s";
  const saveButtonIcon = isSaving ? <CircularProgress size={14} /> : undefined;

  return (
    <DetailPanelContainer>
      <DetailPanelHeader subtitle="Configure rule triggers, channels, and schedules for this recordset." />
      <DetailPanelContent>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          {saveError && <Alert severity="error">{saveError}</Alert>}
          <NotificationRulesEditor
            rules={rules}
            statuses={statuses}
            onAddRule={handleAddRule}
            onUpdateRule={handleUpdateRule}
            onRemoveRule={handleRemoveRule}
          />
        </Stack>
      </DetailPanelContent>
      <DetailPanelFooter>
        <Stack
          direction="row"
          spacing={1.5}
          alignItems="center"
          justifyContent="space-between"
          sx={{ width: "100%" }}
        >
          <Typography variant="caption" color="text.secondary">
            {rules.length} rule{rulesLabelSuffix} configured
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button
              variant="text"
              onClick={closeDetailPanel}
              disabled={isSaving}
            >
              Cancel
            </Button>
            <Button
              variant="contained"
              onClick={handleSave}
              disabled={isSaving}
              startIcon={saveButtonIcon}
            >
              Save rules
            </Button>
          </Stack>
        </Stack>
      </DetailPanelFooter>
    </DetailPanelContainer>
  );
};

export default NotificationRulesDrawer;
