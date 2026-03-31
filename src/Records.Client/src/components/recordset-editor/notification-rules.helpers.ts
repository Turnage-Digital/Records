import type {
  NotificationRuleFormValue,
  NotificationRuleSubmission,
} from "./recordset-editor.types";
import type {
  NotificationChannel,
  NotificationChannelType,
  NotificationRule,
  NotificationScheduleType,
  NotificationTriggerType,
} from "../../models/notification-rule";

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

export const stripClientFields = (
  rule: NotificationRuleFormValue,
): NotificationRuleSubmission => ({
  id: rule.id,
  recordsetId: rule.recordsetId,
  trigger: rule.trigger,
  channels: rule.channels,
  schedule: rule.schedule,
  isActive: rule.isActive,
});

const triggerTypes: ReadonlyArray<NotificationTriggerType> = [
  "RecordCreated",
  "RecordDeleted",
  "RecordUpdated",
  "StatusChanged",
  "ColumnValueChanged",
  "RecordsetDeleted",
  "RecordsetUpdated",
  "CustomCondition",
] as const;

const scheduleTypes: ReadonlyArray<NotificationScheduleType> = [
  "Immediate",
  "Delayed",
  "Daily",
  "Weekly",
  "Batched",
  "Custom",
] as const;

const channelTypes: ReadonlyArray<NotificationChannelType> = [
  "Email",
  "Sms",
  "InApp",
  "Push",
  "Webhook",
] as const;

const weekDayOptions = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
] as const;

const normalizeEnumValue = <T extends string>(
  value: string | null | undefined,
  allowed: ReadonlyArray<T>,
): T | undefined => {
  if (!value) {
    return undefined;
  }

  const direct = allowed.find((option) => option === value);
  if (direct) {
    return direct;
  }

  const lower = value.toLowerCase();
  const lowerMatch = allowed.find((option) => option.toLowerCase() === lower);
  if (lowerMatch) {
    return lowerMatch;
  }

  const capitalized = value.charAt(0).toUpperCase() + value.slice(1);
  return allowed.find((option) => option === capitalized);
};

export const toNotificationRuleFormValue = (
  rule: NotificationRule,
): NotificationRuleFormValue => ({
  id: rule.id ?? undefined,
  recordsetId: rule.recordsetId,
  trigger: {
    ...rule.trigger,
    type:
      normalizeEnumValue(rule.trigger.type, triggerTypes) ?? "RecordCreated",
  },
  channels: (() => {
    const channelMap = new Map<NotificationChannelType, NotificationChannel>();

    for (const channel of rule.channels) {
      const normalizedType =
        normalizeEnumValue(channel.type as string, channelTypes) ??
        channel.type;

      const nextChannel: NotificationChannel = {
        ...channel,
        type: normalizedType,
      };

      const existing = channelMap.get(normalizedType);
      if (!existing) {
        channelMap.set(normalizedType, nextChannel);
        continue;
      }

      const prefersNext =
        (existing.address ?? "").trim() === "" &&
        (nextChannel.address ?? "").trim() !== "";
      if (prefersNext) {
        channelMap.set(normalizedType, nextChannel);
      }
    }

    return Array.from(channelMap.values());
  })(),
  schedule: (() => {
    const normalizedType =
      normalizeEnumValue(rule.schedule.type as string, scheduleTypes) ??
      "Immediate";
    const normalizedDays = Array.isArray(rule.schedule.daysOfWeek)
      ? rule.schedule.daysOfWeek.map(
          (day) => normalizeEnumValue(day, weekDayOptions) ?? day,
        )
      : undefined;

    return {
      ...rule.schedule,
      type: normalizedType,
      daysOfWeek: normalizedDays,
    };
  })(),
  isActive: rule.isActive ?? true,
  clientId: createClientId(),
});

export const createEmptyRuleFormValue = (): NotificationRuleFormValue => ({
  clientId: createClientId(),
  trigger: { type: "RecordCreated" },
  channels: [],
  schedule: { type: "Immediate" },
  isActive: true,
});
