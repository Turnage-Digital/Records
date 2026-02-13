export type MigrationJobStage =
  | "Pending"
  | "Running"
  | "Completed"
  | "Failed"
  | "Archived";

export interface MigrationProgressRecord {
  recordsetId: string;
  correlationId: string;
  stage: MigrationJobStage;
  requestedBy?: string;
  createdOn?: string;
  startedOn?: string;
  completedOn?: string;
  backupExpiresOn?: string;
  backupRemovedOn?: string;
  attempts?: number;
  lastError?: string;
  lastMessage?: string;
  percent?: number;
  itemsProcessed?: number;
  updatedAt?: string;
}
