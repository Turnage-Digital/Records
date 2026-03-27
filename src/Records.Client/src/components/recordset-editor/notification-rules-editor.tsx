import * as React from "react";

import { Add, Delete, MailOutline, Sms as SmsIcon } from "@mui/icons-material";
import {
  Box,
  Button,
  Checkbox,
  Divider,
  FormControl,
  FormControlLabel,
  FormGroup,
  FormHelperText,
  InputLabel,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  Stack,
  Switch,
  TextField,
  Typography,
} from "@mui/material";

import {
  NotificationChannel,
  NotificationChannelType,
  NotificationSchedule,
  NotificationScheduleType,
  NotificationTrigger,
  NotificationTriggerType,
  Status,
} from "../../models";

import type { NotificationRuleFormValue } from "./recordset-editor.types";

const weekDayOptions = [
  "Sunday",
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
] as const;

interface Props {
  rules: NotificationRuleFormValue[];
  statuses: Status[];
  onAddRule: () => void;
  onUpdateRule: (
    clientId: string,
    updater: (rule: NotificationRuleFormValue) => NotificationRuleFormValue,
  ) => void;
  onRemoveRule: (rule: NotificationRuleFormValue) => void;
}

interface TriggerOption {
  value: Exclude<NotificationTriggerType, "CustomCondition">;
  label: string;
}

const triggerOptions: TriggerOption[] = [
  { label: "Record created", value: "RecordCreated" },
  { label: "Record updated", value: "RecordUpdated" },
  { label: "Record deleted", value: "RecordDeleted" },
  { label: "Status changed", value: "StatusChanged" },
  { label: "Column value changed", value: "ColumnValueChanged" },
  { label: "Recordset updated", value: "RecordsetUpdated" },
  { label: "Recordset deleted", value: "RecordsetDeleted" },
];

const triggerDescriptions: Partial<Record<NotificationTriggerType, string>> = {
  StatusChanged: "Watch records when they move between workflow statuses.",
  ColumnValueChanged:
    "Monitor a specific column for changes between optional from/to values.",
  RecordsetUpdated:
    "Capture edits to the recordset itself, including metadata changes.",
  RecordsetDeleted: "Alert when the entire recordset is removed.",
};

interface ChannelOption {
  value: NotificationChannelType;
  label: string;
  icon: React.ReactNode;
  requiresAddress: boolean;
  addressLabel?: string;
  addressPlaceholder?: string;
}

const channelOptions: ChannelOption[] = [
  {
    label: "Email",
    value: "Email",
    icon: <MailOutline fontSize="small" />,
    requiresAddress: true,
    addressLabel: "Email address",
    addressPlaceholder: "name@example.com",
  },
  {
    label: "SMS",
    value: "Sms",
    icon: <SmsIcon fontSize="small" />,
    requiresAddress: true,
    addressLabel: "Phone number",
    addressPlaceholder: "+1 555-0100",
  },
];

const scheduleOptions: {
  label: string;
  value: NotificationScheduleType;
}[] = [
  { label: "Immediately", value: "Immediate" },
  { label: "Delayed", value: "Delayed" },
  { label: "Daily", value: "Daily" },
  { label: "Weekly", value: "Weekly" },
];

const scheduleDescriptions: Partial<Record<NotificationScheduleType, string>> =
  {
    Immediate: "Send notifications as soon as the trigger fires.",
    Delayed: "Wait for a delay before sending.",
    Daily: "Send once per day at the specified time.",
    Weekly: "Send on specific days each week.",
  };

const ensureSettings = (channel: NotificationChannel): NotificationChannel => ({
  ...channel,
  settings: channel.settings ?? {},
});

const NotificationRulesEditor = ({
  rules,
  statuses,
  onAddRule,
  onRemoveRule,
  onUpdateRule,
}: Props) => {
  const statusNames = React.useMemo(
    () => statuses.map((status) => status.name),
    [statuses],
  );
  const handleTriggerTypeChange = (
    rule: NotificationRuleFormValue,
    nextType: NotificationTriggerType,
  ) => {
    const nextTrigger: NotificationTrigger = { type: nextType };

    if (nextType === "StatusChanged") {
      nextTrigger.fromValue = null;
      nextTrigger.toValue = null;
    }

    if (nextType === "ColumnValueChanged") {
      nextTrigger.columnName = "";
      nextTrigger.fromValue = null;
      nextTrigger.toValue = null;
    }

    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      trigger: nextTrigger,
    }));
  };

  const handleTriggerStatusChange = (
    rule: NotificationRuleFormValue,
    field: "fromValue" | "toValue",
    value: string | null,
  ) => {
    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      trigger: {
        ...prev.trigger,
        [field]: value,
      },
    }));
  };

  const handleTriggerColumnChange = (
    rule: NotificationRuleFormValue,
    field: "columnName" | "fromValue" | "toValue",
    value: string,
  ) => {
    const trimmed = value.trim();
    let nextValue: string | null;
    if (trimmed === "") {
      nextValue = field === "columnName" ? "" : null;
    } else {
      nextValue = trimmed;
    }
    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      trigger: {
        ...prev.trigger,
        [field]: nextValue,
      },
    }));
  };

  const handleChannelToggle = (
    rule: NotificationRuleFormValue,
    channelType: NotificationChannelType,
    checked: boolean,
  ) => {
    onUpdateRule(rule.clientId, (prev) => {
      const existing = prev.channels;
      if (checked) {
        if (existing.some((channel) => channel.type === channelType)) {
          return prev;
        }
        const option = channelOptions.find(
          (item) => item.value === channelType,
        );
        const nextChannel: NotificationChannel = ensureSettings({
          type: channelType,
          address: undefined,
        });
        if (option?.requiresAddress) {
          nextChannel.address = "";
        }
        return {
          ...prev,
          channels: [...existing, nextChannel],
        };
      }

      return {
        ...prev,
        channels: existing.filter((channel) => channel.type !== channelType),
      };
    });
  };

  const handleChannelAddressChange = (
    rule: NotificationRuleFormValue,
    channelType: NotificationChannelType,
    address: string,
  ) => {
    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      channels: prev.channels.map((channel) =>
        channel.type === channelType ? { ...channel, address } : channel,
      ),
    }));
  };

  const updateSchedule = (
    rule: NotificationRuleFormValue,
    updates: Partial<NotificationSchedule>,
  ) => {
    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      schedule: {
        ...prev.schedule,
        ...updates,
      },
    }));
  };

  const handleScheduleChange = (
    rule: NotificationRuleFormValue,
    scheduleType: NotificationScheduleType,
  ) => {
    onUpdateRule(rule.clientId, (prev) => {
      const previous = prev.schedule;
      const nextSchedule: NotificationSchedule = { type: scheduleType };

      if (scheduleType === "Delayed") {
        nextSchedule.delay =
          previous.type === "Delayed" ? previous.delay : undefined;
      }

      if (scheduleType === "Daily") {
        nextSchedule.dailyAt =
          previous.type === "Daily" ? previous.dailyAt : undefined;
      }

      if (scheduleType === "Weekly") {
        nextSchedule.daysOfWeek =
          previous.type === "Weekly" ? previous.daysOfWeek : [];
        nextSchedule.dailyAt =
          previous.type === "Weekly" ? previous.dailyAt : undefined;
      }

      return {
        ...prev,
        schedule: nextSchedule,
      };
    });
  };

  const handleActiveChange = (
    rule: NotificationRuleFormValue,
    active: boolean,
  ) => {
    onUpdateRule(rule.clientId, (prev) => ({
      ...prev,
      isActive: active,
    }));
  };

  const handleWeeklyDaysChange = (
    rule: NotificationRuleFormValue,
    days: string[],
  ) => {
    updateSchedule(rule, {
      daysOfWeek: days,
    });
  };

  const renderTriggerDetails = (rule: NotificationRuleFormValue) => {
    const triggerType = rule.trigger.type;

    if (triggerType === "StatusChanged") {
      return (
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={{ xs: 2, md: 3 }}
        >
          <FormControl fullWidth>
            <InputLabel id={`from-${rule.clientId}`}>From status</InputLabel>
            <Select
              labelId={`from-${rule.clientId}`}
              label="From status"
              value={rule.trigger.fromValue ?? ""}
              onChange={(event) =>
                handleTriggerStatusChange(
                  rule,
                  "fromValue",
                  event.target.value === ""
                    ? null
                    : (event.target.value as string),
                )
              }
              sx={{ backgroundColor: "background.paper" }}
            >
              <MenuItem value="">
                <em>Any</em>
              </MenuItem>
              {statusNames.map((name) => (
                <MenuItem key={name} value={name}>
                  {name}
                </MenuItem>
              ))}
            </Select>
            <FormHelperText>
              Leave as Any to match all starting statuses.
            </FormHelperText>
          </FormControl>

          <FormControl fullWidth>
            <InputLabel id={`to-${rule.clientId}`}>To status</InputLabel>
            <Select
              labelId={`to-${rule.clientId}`}
              label="To status"
              value={rule.trigger.toValue ?? ""}
              onChange={(event) =>
                handleTriggerStatusChange(
                  rule,
                  "toValue",
                  event.target.value === ""
                    ? null
                    : (event.target.value as string),
                )
              }
              sx={{ backgroundColor: "background.paper" }}
            >
              <MenuItem value="">
                <em>Any</em>
              </MenuItem>
              {statusNames.map((name) => (
                <MenuItem key={name} value={name}>
                  {name}
                </MenuItem>
              ))}
            </Select>
            <FormHelperText>
              Choose a destination status to watch for, or Any.
            </FormHelperText>
          </FormControl>
        </Stack>
      );
    }

    if (triggerType === "ColumnValueChanged") {
      return (
        <Stack spacing={2}>
          <TextField
            label="Column name"
            value={rule.trigger.columnName ?? ""}
            onChange={(event) =>
              handleTriggerColumnChange(rule, "columnName", event.target.value)
            }
            placeholder="Enter a column key or name"
            fullWidth
            InputProps={{
              sx: { backgroundColor: "background.paper" },
            }}
          />
          <Stack
            direction={{ xs: "column", md: "row" }}
            spacing={{ xs: 2, md: 3 }}
          >
            <TextField
              label="From value (optional)"
              value={rule.trigger.fromValue ?? ""}
              onChange={(event) =>
                handleTriggerColumnChange(rule, "fromValue", event.target.value)
              }
              placeholder="Previous value"
              fullWidth
              InputProps={{
                sx: { backgroundColor: "background.paper" },
              }}
            />
            <TextField
              label="To value (optional)"
              value={rule.trigger.toValue ?? ""}
              onChange={(event) =>
                handleTriggerColumnChange(rule, "toValue", event.target.value)
              }
              placeholder="New value"
              fullWidth
              InputProps={{
                sx: { backgroundColor: "background.paper" },
              }}
            />
          </Stack>
          <FormHelperText>
            Leave either field blank to match any previous or next value.
          </FormHelperText>
        </Stack>
      );
    }

    if (
      triggerType === "RecordsetUpdated" ||
      triggerType === "RecordsetDeleted"
    ) {
      return (
        <Typography color="text.secondary">
          Applies to recordset-level events. No additional filters are required.
        </Typography>
      );
    }

    return null;
  };

  const renderScheduleDetails = (rule: NotificationRuleFormValue) => {
    const scheduleType = rule.schedule.type;

    if (scheduleType === "Delayed") {
      const rawDelay = rule.schedule.delay;
      const parsedDelay = Number.parseInt(String(rawDelay ?? ""), 10);
      const delayMinutes =
        Number.isFinite(parsedDelay) && parsedDelay >= 0
          ? parsedDelay
          : undefined;
      const delayValue = delayMinutes == null ? "" : String(delayMinutes);
      return (
        <TextField
          label="Delay (minutes)"
          type="number"
          value={delayValue}
          onChange={(event) => {
            const nextValue = event.target.value;
            const numeric = Number.parseInt(nextValue, 10);
            updateSchedule(rule, {
              delay:
                Number.isFinite(numeric) && numeric >= 0
                  ? String(numeric)
                  : undefined,
            });
          }}
          placeholder="15"
          InputProps={{
            sx: { backgroundColor: "background.paper" },
            inputProps: { min: 0 },
          }}
        />
      );
    }

    if (scheduleType === "Batched") {
      return (
        <Typography color="text.secondary" variant="body2">
          Existing batched schedules can be viewed here but not created from the
          editor yet.
        </Typography>
      );
    }

    if (scheduleType === "Daily") {
      return (
        <TextField
          label="Send at"
          type="time"
          value={rule.schedule.dailyAt ?? ""}
          onChange={(event) =>
            updateSchedule(rule, {
              dailyAt: event.target.value ? event.target.value : undefined,
            })
          }
          helperText="Choose a time of day (HH:mm)."
          InputLabelProps={{ shrink: true }}
          InputProps={{ sx: { backgroundColor: "background.paper" } }}
        />
      );
    }

    if (scheduleType === "Weekly") {
      const selectedDays = rule.schedule.daysOfWeek ?? [];
      return (
        <Stack spacing={2}>
          <FormControl fullWidth>
            <InputLabel id={`weekly-days-${rule.clientId}`}>
              Days of week
            </InputLabel>
            <Select
              labelId={`weekly-days-${rule.clientId}`}
              multiple
              value={selectedDays}
              onChange={(event) => {
                const value = event.target.value;
                const normalized = Array.isArray(value)
                  ? value
                  : value
                      .split(",")
                      .map((day) => day.trim())
                      .filter(Boolean);
                handleWeeklyDaysChange(rule, normalized as string[]);
              }}
              input={<OutlinedInput label="Days of week" />}
              renderValue={(selected) =>
                (selected as string[]).length > 0
                  ? (selected as string[]).join(", ")
                  : "Select days"
              }
              sx={{ backgroundColor: "background.paper" }}
            >
              {weekDayOptions.map((day) => (
                <MenuItem key={day} value={day}>
                  <Checkbox checked={selectedDays.includes(day)} />
                  <Typography>{day}</Typography>
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Send at"
            type="time"
            value={rule.schedule.dailyAt ?? ""}
            onChange={(event) =>
              updateSchedule(rule, {
                dailyAt: event.target.value ? event.target.value : undefined,
              })
            }
            helperText="Time of day to send (HH:mm)."
            InputLabelProps={{ shrink: true }}
            InputProps={{ sx: { backgroundColor: "background.paper" } }}
          />
        </Stack>
      );
    }

    return null;
  };

  const rulesView =
    rules.length === 0 ? (
      <Typography color="text.secondary">
        No notification rules configured yet.
      </Typography>
    ) : (
      <Stack spacing={3.5}>
        {rules.map((rule, index) => {
          const enabledChannels = new Set(
            rule.channels.map((channel) => channel.type),
          );
          const triggerDetail = renderTriggerDetails(rule);
          const scheduleDetail = renderScheduleDetails(rule);
          const triggerDescription = triggerDescriptions[rule.trigger.type];
          const scheduleDescription = scheduleDescriptions[rule.schedule.type];
          const isActive = rule.isActive;
          const activeLabel = isActive ? "Active" : "Inactive";

          const addressableChannels = rule.channels
            .map((channel) => {
              const option = channelOptions.find(
                (candidate) => candidate.value === channel.type,
              );
              if (!option || !option.requiresAddress) {
                return null;
              }
              return { channel, option };
            })
            .filter(
              (
                value,
              ): value is {
                channel: NotificationChannel;
                option: ChannelOption;
              } => Boolean(value),
            );

          return (
            <Paper
              key={rule.clientId}
              variant="outlined"
              sx={{
                p: { xs: 3, md: 3.5 },
                borderRadius: (theme) => theme.shape.borderRadius,
                boxShadow: "none",
                borderColor: (theme) => theme.palette.divider,
              }}
            >
              <Stack spacing={3} divider={<Divider flexItem />}>
                <Stack
                  direction={{ xs: "column", md: "row" }}
                  spacing={2}
                  alignItems={{ xs: "flex-start", md: "center" }}
                  justifyContent="space-between"
                >
                  <Typography fontWeight={600}>Rule {index + 1}</Typography>
                  <Stack direction="row" spacing={2} alignItems="center">
                    <FormControlLabel
                      control={
                        <Switch
                          checked={isActive}
                          onChange={(event) =>
                            handleActiveChange(rule, event.target.checked)
                          }
                          inputProps={{
                            "aria-label": `Toggle rule ${index + 1} active`,
                          }}
                        />
                      }
                      label={activeLabel}
                    />
                    <Button
                      color="error"
                      startIcon={<Delete />}
                      onClick={() => onRemoveRule(rule)}
                      size="small"
                    >
                      Remove
                    </Button>
                  </Stack>
                </Stack>

                <Stack spacing={2}>
                  <FormControl fullWidth>
                    <InputLabel id={`trigger-${rule.clientId}`}>
                      Trigger
                    </InputLabel>
                    <Select
                      labelId={`trigger-${rule.clientId}`}
                      label="Trigger"
                      value={rule.trigger.type}
                      onChange={(event) =>
                        handleTriggerTypeChange(
                          rule,
                          event.target.value as NotificationTriggerType,
                        )
                      }
                      sx={{ backgroundColor: "background.paper" }}
                    >
                      {triggerOptions.map((option) => (
                        <MenuItem key={option.value} value={option.value}>
                          {option.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  {triggerDescription && (
                    <Typography color="text.secondary" variant="body2">
                      {triggerDescription}
                    </Typography>
                  )}
                  {triggerDetail}
                </Stack>

                <Stack spacing={2}>
                  <Box>
                    <Typography fontWeight={600} gutterBottom>
                      Channels
                    </Typography>
                    <FormGroup
                      row
                      sx={{
                        gap: 2,
                        flexWrap: "wrap",
                      }}
                    >
                      {channelOptions.map((option) => (
                        <FormControlLabel
                          key={option.value}
                          control={
                            <Checkbox
                              checked={enabledChannels.has(option.value)}
                              onChange={(event) =>
                                handleChannelToggle(
                                  rule,
                                  option.value,
                                  event.target.checked,
                                )
                              }
                            />
                          }
                          label={
                            <Stack
                              direction="row"
                              spacing={1}
                              alignItems="center"
                            >
                              {option.icon}
                              <span>{option.label}</span>
                            </Stack>
                          }
                        />
                      ))}
                    </FormGroup>
                  </Box>
                  {addressableChannels.length > 0 && (
                    <Stack spacing={2}>
                      {addressableChannels.map(({ channel, option }) => (
                        <TextField
                          key={`${rule.clientId}-${option.value}`}
                          label={
                            option.addressLabel ?? `${option.label} details`
                          }
                          value={channel.address ?? ""}
                          onChange={(event) =>
                            handleChannelAddressChange(
                              rule,
                              channel.type,
                              event.target.value,
                            )
                          }
                          placeholder={option.addressPlaceholder}
                          InputProps={{
                            sx: { backgroundColor: "background.paper" },
                          }}
                        />
                      ))}
                    </Stack>
                  )}
                </Stack>

                <Stack spacing={2}>
                  <FormControl fullWidth>
                    <InputLabel id={`schedule-${rule.clientId}`}>
                      Schedule
                    </InputLabel>
                    <Select
                      labelId={`schedule-${rule.clientId}`}
                      label="Schedule"
                      value={rule.schedule.type}
                      onChange={(event) =>
                        handleScheduleChange(
                          rule,
                          event.target.value as NotificationScheduleType,
                        )
                      }
                      sx={{ backgroundColor: "background.paper" }}
                    >
                      {scheduleOptions.map((option) => (
                        <MenuItem key={option.value} value={option.value}>
                          {option.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  {scheduleDescription && (
                    <Typography color="text.secondary" variant="body2">
                      {scheduleDescription}
                    </Typography>
                  )}
                  {scheduleDetail}
                </Stack>
              </Stack>
            </Paper>
          );
        })}
      </Stack>
    );
  return (
    <Stack spacing={2.5}>
      <Box>
        <Button
          onClick={onAddRule}
          startIcon={<Add />}
          variant="outlined"
          size="small"
        >
          Add rule
        </Button>
      </Box>

      {rulesView}
    </Stack>
  );
};

export default NotificationRulesEditor;
