import { ClockState } from "./clock-state";

export interface Clock {
  clockId: string;
  tenantId: string;
  recordsetId: string;
  recordId: number;
  definitionId: string;
  state: ClockState;
  startedAt: string;
  atRiskDueAt: string;
  breachDueAt: string;
  atRiskAt: string | null;
  breachedAt: string | null;
  pausedAt: string | null;
  completedAt: string | null;
  pauseReason: string | null;
  accumulatedPauseTime: string;
}
