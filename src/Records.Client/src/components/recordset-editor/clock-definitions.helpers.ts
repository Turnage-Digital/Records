import type {
  ClockDefinitionFormValue,
  ClockDefinitionSubmission,
} from "./recordset-editor.types";
import type { ClockDefinition } from "../../models/clock-definition";

const createClientId = () => {
  const cryptoApi = globalThis.crypto;
  const hasRandomUuid =
    typeof cryptoApi !== "undefined" &&
    typeof cryptoApi.randomUUID === "function";

  if (hasRandomUuid) {
    return cryptoApi.randomUUID();
  }

  return Math.random().toString(36).slice(2);
};

export const createEmptyClockDefinitionFormValue = (
  tenantId?: string | null,
): ClockDefinitionFormValue => ({
  tenantId: tenantId ?? undefined,
  name: "",
  atRiskThresholdValue: 4,
  atRiskThresholdUnit: "Hours",
  breachThresholdValue: 8,
  breachThresholdUnit: "Hours",
  isActive: true,
  clientId: createClientId(),
});

export const toClockDefinitionFormValue = (
  definition: ClockDefinition,
): ClockDefinitionFormValue => ({
  id: definition.definitionId,
  tenantId: definition.tenantId,
  name: definition.name,
  atRiskThresholdValue: definition.atRiskThresholdValue,
  atRiskThresholdUnit: definition.atRiskThresholdUnit,
  breachThresholdValue: definition.breachThresholdValue,
  breachThresholdUnit: definition.breachThresholdUnit,
  isActive: definition.isActive,
  clientId: createClientId(),
});

export const stripClockDefinitionClientFields = (
  definition: ClockDefinitionFormValue,
): ClockDefinitionSubmission => ({
  id: definition.id,
  tenantId: definition.tenantId,
  name: definition.name,
  atRiskThresholdValue: definition.atRiskThresholdValue,
  atRiskThresholdUnit: definition.atRiskThresholdUnit,
  breachThresholdValue: definition.breachThresholdValue,
  breachThresholdUnit: definition.breachThresholdUnit,
  isActive: definition.isActive,
});
