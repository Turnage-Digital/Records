export type NotificationTriggerType =
  | "RecordCreated"
  | "RecordDeleted"
  | "RecordUpdated"
  | "StatusChanged"
  | "ColumnValueChanged"
  | "RecordsetDeleted"
  | "RecordsetUpdated"
  | "CustomCondition";

export interface NotificationTrigger {
  type: NotificationTriggerType;
  fromValue?: string | null;
  toValue?: string | null;
  columnName?: string | null;
  operator?: string | null;
  value?: string | null;
}

export type NotificationChannelType =
  | "Email"
  | "Sms"
  | "InApp"
  | "Push"
  | "Webhook";

export interface NotificationChannel {
  type: NotificationChannelType;
  address?: string | null;
  settings?: Record<string, string>;
}

export type NotificationScheduleType =
  | "Immediate"
  | "Delayed"
  | "Daily"
  | "Weekly"
  | "Batched"
  | "Custom";

export interface NotificationSchedule {
  type: NotificationScheduleType;
  delay?: string | null;
  cronExpression?: string | null;
  dailyAt?: string | null;
  daysOfWeek?: string[] | null;
}

export interface NotificationRule {
  id?: string;
  recordsetId: string;
  userId: string;
  trigger: NotificationTrigger;
  channels: NotificationChannel[];
  schedule: NotificationSchedule;
  isActive?: boolean;
}

export interface NotificationRuleInput {
  trigger: NotificationTrigger;
  channels: NotificationChannel[];
  schedule: NotificationSchedule;
  isActive: boolean;
}
