import { ClockThresholdUnit } from "./clock-threshold-unit";

export interface ClockDefinition {
  definitionId: string;
  tenantId: string;
  name: string;
  atRiskThresholdValue: number;
  atRiskThresholdUnit: ClockThresholdUnit;
  breachThresholdValue: number;
  breachThresholdUnit: ClockThresholdUnit;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}
