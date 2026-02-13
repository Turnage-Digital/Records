import {
  ClockThresholdUnit,
  Column,
  RecordsetItemDefinition,
  MigrationPlan,
  NotificationRuleInput,
  Status,
  StatusTransition,
} from "../../models";

export interface NotificationRuleFormValue extends NotificationRuleInput {
  id?: string;
  recordsetId?: string;
  clientId: string;
}

export interface NotificationRuleSubmission extends NotificationRuleInput {
  id?: string;
  recordsetId?: string;
}

export interface ClockDefinitionFormValue {
  id?: string;
  tenantId?: string;
  name: string;
  atRiskThresholdValue: number;
  atRiskThresholdUnit: ClockThresholdUnit;
  breachThresholdValue: number;
  breachThresholdUnit: ClockThresholdUnit;
  isActive: boolean;
  clientId: string;
}

export interface ClockDefinitionSubmission {
  id?: string;
  tenantId?: string;
  name: string;
  atRiskThresholdValue: number;
  atRiskThresholdUnit: ClockThresholdUnit;
  breachThresholdValue: number;
  breachThresholdUnit: ClockThresholdUnit;
  isActive: boolean;
}

export interface RecordsetEditorInitialValue {
  id?: string | null;
  name: string;
  columns: Column[];
  statuses: Status[];
  transitions: StatusTransition[];
}

export interface RecordsetEditorSubmitResult {
  definition: RecordsetItemDefinition;
}

export class RecordsetMigrationRequiredError extends Error {
  public readonly reasons: string[];
  public readonly plan?: MigrationPlan;

  constructor(message: string, reasons: string[], plan?: MigrationPlan) {
    super(message);
    this.name = "RecordsetMigrationRequiredError";
    this.reasons = reasons;
    this.plan = plan;
  }
}
