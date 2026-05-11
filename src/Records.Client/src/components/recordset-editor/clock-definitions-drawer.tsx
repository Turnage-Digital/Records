import * as React from "react";

import {
  Alert,
  Button,
  CircularProgress,
  Stack,
  Typography,
} from "@mui/material";

import ClockDefinitionsEditor from "./clock-definitions-editor";
import DetailPanelContainer from "../detail-panel/detail-panel-container";
import DetailPanelContent from "../detail-panel/detail-panel-content";
import DetailPanelFooter from "../detail-panel/detail-panel-footer";
import DetailPanelHeader from "../detail-panel/detail-panel-header";
import useDetailPanel from "../detail-panel/use-detail-panel";

import type { ClockDefinitionFormValue } from "./recordset-editor.types";

interface ClockDefinitionsDrawerProps {
  tenantId?: string | null;
  initialDefinitions: ClockDefinitionFormValue[];
  initialDisabledDefinitionIds: string[];
  onSave: (
    definitions: ClockDefinitionFormValue[],
    disabledDefinitionIds: string[],
  ) => Promise<void> | void;
}

const cloneDefinition = (
  definition: ClockDefinitionFormValue,
): ClockDefinitionFormValue => ({
  ...definition,
});

const ClockDefinitionsDrawer = ({
  tenantId,
  initialDefinitions,
  initialDisabledDefinitionIds,
  onSave,
}: ClockDefinitionsDrawerProps) => {
  const { closeDetailPanel } = useDetailPanel();
  const [definitions, setDefinitions] = React.useState<
    ClockDefinitionFormValue[]
  >(() => initialDefinitions.map(cloneDefinition));
  const [disabledDefinitionIds, setDisabledDefinitionIds] = React.useState<
    string[]
  >(initialDisabledDefinitionIds);
  const [saveError, setSaveError] = React.useState<string | null>(null);
  const [isSaving, setIsSaving] = React.useState(false);

  const handleAddDefinition = (definition: ClockDefinitionFormValue) => {
    setDefinitions((previous) => [...previous, definition]);
  };

  const handleUpdateDefinition = (definition: ClockDefinitionFormValue) => {
    setDefinitions((previous) =>
      previous.map((existing) =>
        existing.clientId === definition.clientId ? definition : existing,
      ),
    );
  };

  const handleRemoveDefinition = (definition: ClockDefinitionFormValue) => {
    setDefinitions((previous) =>
      previous.filter((existing) => existing.clientId !== definition.clientId),
    );
    if (definition.id) {
      const definitionId = definition.id;
      setDisabledDefinitionIds((previous) =>
        previous.includes(definitionId)
          ? previous
          : [...previous, definitionId],
      );
    }
  };

  const handleSave = async () => {
    setSaveError(null);
    setIsSaving(true);
    try {
      await onSave(definitions.map(cloneDefinition), disabledDefinitionIds);
      closeDetailPanel();
    } catch (error) {
      if (error instanceof Error && error.message.trim().length > 0) {
        setSaveError(error.message);
      } else {
        setSaveError("Failed to save clock definitions.");
      }
    } finally {
      setIsSaving(false);
    }
  };
  const definitionsLabelSuffix = definitions.length === 1 ? "" : "s";
  const saveButtonIcon = isSaving ? <CircularProgress size={14} /> : undefined;

  return (
    <DetailPanelContainer>
      <DetailPanelHeader subtitle="Configure reusable timers used when clocks are started on records." />
      <DetailPanelContent>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          {saveError && <Alert severity="error">{saveError}</Alert>}
          <ClockDefinitionsEditor
            tenantId={tenantId}
            definitions={definitions}
            onAddDefinition={handleAddDefinition}
            onUpdateDefinition={handleUpdateDefinition}
            onRemoveDefinition={handleRemoveDefinition}
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
            {definitions.length} definition
            {definitionsLabelSuffix} configured
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
              Save clocks
            </Button>
          </Stack>
        </Stack>
      </DetailPanelFooter>
    </DetailPanelContainer>
  );
};

export default ClockDefinitionsDrawer;
