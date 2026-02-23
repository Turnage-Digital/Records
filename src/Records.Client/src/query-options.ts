import { infiniteQueryOptions, queryOptions } from "@tanstack/react-query";

import {
  Clock,
  ClockDefinition,
  HistoryPage,
  MigrationProgressRecord,
  NotificationDetails,
  NotificationPage,
  NotificationRule,
  NotificationsSearch,
  RecordDetails,
  RecordsetItemDefinition,
  RecordsetName,
  RecordsetPagedRecords,
  RecordsetSearch,
  TenantSummary,
  UserRoleMembership,
  UserSummary,
} from "./models";

const throwIfNotOk = async (response: Response, message: string) => {
  if (response.ok) {
    return response;
  }

  const text = await response.text().catch(() => undefined);
  throw new Error(text || message);
};

const readString = (
  input: Record<string, unknown>,
  key: string,
): string | undefined => {
  const value = input[key];
  return typeof value === "string" && value.length > 0 ? value : undefined;
};

const readNumber = (
  input: Record<string, unknown>,
  key: string,
): number | undefined => {
  const value = input[key];
  return typeof value === "number" ? value : undefined;
};

const normalizeNotificationSummary = (
  raw: Record<string, unknown>,
): NotificationPage["notifications"][number] => ({
  id: readString(raw, "id") ?? "",
  title: readString(raw, "title") ?? "",
  body: readString(raw, "body") ?? "",
  isRead: raw.isRead === true,
  occurredOn: readString(raw, "occurredOn") ?? new Date().toISOString(),
  recordsetId: readString(raw, "recordsetId"),
  recordId: readNumber(raw, "recordId"),
  metadata:
    typeof raw.metadata === "object" && raw.metadata !== null
      ? (raw.metadata as Record<string, unknown>)
      : undefined,
});

const normalizeNotificationDetails = (
  raw: Record<string, unknown>,
): NotificationDetails => ({
  id: readString(raw, "id") ?? "",
  userId: readString(raw, "userId") ?? "",
  recordsetId: readString(raw, "recordsetId"),
  recordId: readNumber(raw, "recordId"),
  title: readString(raw, "title") ?? "",
  body: readString(raw, "body") ?? "",
  isRead: raw.isRead === true,
  metadata:
    typeof raw.metadata === "object" && raw.metadata !== null
      ? (raw.metadata as Record<string, unknown>)
      : undefined,
  history: Array.isArray(raw.history)
    ? (raw.history as NotificationDetails["history"])
    : [],
  deliveryAttempts: Array.isArray(raw.deliveryAttempts)
    ? (raw.deliveryAttempts as NotificationDetails["deliveryAttempts"])
    : [],
});

const normalizeNotificationRule = (
  raw: Record<string, unknown>,
): NotificationRule => ({
  id: readString(raw, "id"),
  recordsetId: readString(raw, "recordsetId") ?? "",
  userId: readString(raw, "userId") ?? "",
  trigger: (raw.trigger ?? {}) as NotificationRule["trigger"],
  channels: Array.isArray(raw.channels)
    ? (raw.channels as NotificationRule["channels"])
    : [],
  schedule: (raw.schedule ?? {}) as NotificationRule["schedule"],
  isActive: raw.isActive === true,
});

const fetchNotificationsPage = async (
  search: NotificationsSearch,
  pageParam: number,
): Promise<NotificationPage> => {
  const params = new URLSearchParams();
  if (search.since) params.set("since", search.since);
  if (typeof search.unread === "boolean")
    params.set("unread", String(search.unread));
  if (search.recordsetId) params.set("recordsetId", search.recordsetId);
  params.set("pageSize", String(search.pageSize ?? 20));
  params.set("page", String(pageParam));

  const response = await fetch(`/api/notifications?${params.toString()}`);
  await throwIfNotOk(response, "Failed to load notifications");
  const raw = (await response.json()) as Record<string, unknown>;
  const notificationsRaw = Array.isArray(raw.notifications)
    ? (raw.notifications as Record<string, unknown>[])
    : [];

  return {
    notifications: notificationsRaw.map(normalizeNotificationSummary),
    totalCount: readNumber(raw, "totalCount") ?? 0,
    unreadCount: readNumber(raw, "unreadCount") ?? 0,
    hasMore: raw.hasMore === true,
  } satisfies NotificationPage;
};

export const notificationsInfiniteQueryOptions = (
  search: NotificationsSearch = {},
) =>
  infiniteQueryOptions<
    NotificationPage,
    Error,
    NotificationPage,
    readonly ["notifications", NotificationsSearch],
    number
  >({
    queryKey: ["notifications", search],
    initialPageParam: search.page ?? 0,
    queryFn: ({ pageParam }) => fetchNotificationsPage(search, pageParam),
    getNextPageParam: (lastPage, allPages) =>
      lastPage.hasMore ? allPages.length : undefined,
  });

const fetchHistoryPage = async (url: string): Promise<HistoryPage> => {
  const response = await fetch(url);
  await throwIfNotOk(response, "Failed to load history");
  return (await response.json()) as HistoryPage;
};

const nextHistoryPageParam = (lastPage: HistoryPage) => {
  const totalPages = Math.ceil(lastPage.total / lastPage.pageSize);
  const next = lastPage.page + 1;
  return next < totalPages ? next : undefined;
};

export const recordsetHistoryInfiniteQueryOptions = (
  recordsetId: string,
  pageSize = 20,
) =>
  infiniteQueryOptions<
    HistoryPage,
    Error,
    HistoryPage,
    readonly ["recordset-history", string, number],
    number
  >({
    queryKey: ["recordset-history", recordsetId, pageSize],
    initialPageParam: 0,
    enabled: Boolean(recordsetId),
    queryFn: ({ pageParam }) =>
      fetchHistoryPage(
        `/api/recordsets/${recordsetId}/history?page=${pageParam}&pageSize=${pageSize}`,
      ),
    getNextPageParam: nextHistoryPageParam,
  });

export const recordHistoryInfiniteQueryOptions = (
  recordsetId: string,
  recordId: number,
  pageSize = 20,
) =>
  infiniteQueryOptions<
    HistoryPage,
    Error,
    HistoryPage,
    readonly ["record-history", string, number, number],
    number
  >({
    queryKey: ["record-history", recordsetId, recordId, pageSize],
    initialPageParam: 0,
    enabled: Boolean(recordsetId) && Number.isInteger(recordId),
    queryFn: ({ pageParam }) =>
      fetchHistoryPage(
        `/api/recordsets/${recordsetId}/records/${recordId}/history?page=${
          typeof pageParam === "number" ? pageParam : 0
        }&pageSize=${pageSize}`,
      ),
    getNextPageParam: nextHistoryPageParam,
  });

export const recordsetNamesQueryOptions = () =>
  queryOptions({
    queryKey: ["recordset-names"],
    queryFn: async () => {
      const response = await fetch("/api/recordsets/names", { method: "GET" });
      await throwIfNotOk(response, "Failed to load recordset names");
      return (await response.json()) as RecordsetName[];
    },
  });

export const recordsetItemDefinitionQueryOptions = (recordsetId?: string) =>
  queryOptions({
    queryKey: ["recordset-definition", recordsetId],
    queryFn: async () => {
      const response = await fetch(
        `/api/recordsets/${recordsetId}/itemDefinition`,
        {
          method: "GET",
        },
      );
      await throwIfNotOk(response, "Failed to load recordset definition");
      return (await response.json()) as RecordsetItemDefinition;
    },
    enabled: Boolean(recordsetId),
  });

export const pagedRecordsQueryOptions = (
  search: RecordsetSearch,
  recordsetId?: string,
) =>
  queryOptions({
    queryKey: [
      "recordset-records",
      recordsetId,
      search.page,
      search.pageSize,
      search.status ?? "__all__",
      search.field ?? "id",
      search.sort ?? "asc",
    ],
    queryFn: async () => {
      let url = `/api/recordsets/${recordsetId}/records?page=${search.page}&pageSize=${search.pageSize}`;
      if (search.status) {
        url += `&status=${encodeURIComponent(search.status)}`;
      }
      if (search.field && search.sort) {
        url += `&field=${search.field}&sort=${search.sort}`;
      }
      const response = await fetch(url, { method: "GET" });
      await throwIfNotOk(response, "Failed to load records");
      return (await response.json()) as RecordsetPagedRecords;
    },
    enabled: Boolean(recordsetId),
  });

export const recordQueryOptions = (recordsetId?: string, recordId?: number) =>
  queryOptions({
    queryKey: ["record", recordsetId, recordId],
    queryFn: async () => {
      const response = await fetch(
        `/api/recordsets/${recordsetId}/records/${recordId}`,
        {
          method: "GET",
        },
      );
      await throwIfNotOk(response, "Failed to load record");
      return (await response.json()) as RecordDetails;
    },
    enabled: Boolean(recordsetId) && Boolean(recordId),
  });

export const tenantSummariesQueryOptions = () =>
  queryOptions({
    queryKey: ["tenant-summaries"],
    queryFn: async () => {
      const response = await fetch("/api/tenants", { method: "GET" });
      await throwIfNotOk(response, "Failed to load tenants");
      return (await response.json()) as TenantSummary[];
    },
  });

export const userSummariesQueryOptions = () =>
  queryOptions({
    queryKey: ["user-summaries"],
    queryFn: async () => {
      const response = await fetch("/api/users", { method: "GET" });
      await throwIfNotOk(response, "Failed to load users");
      return (await response.json()) as UserSummary[];
    },
  });

export const userRoleMembershipsQueryOptions = (userId?: string) =>
  queryOptions({
    queryKey: ["user-role-memberships", userId ?? null],
    enabled: Boolean(userId),
    queryFn: async () => {
      if (!userId) {
        return [] as UserRoleMembership[];
      }

      const response = await fetch(`/api/users/${userId}/roles`, {
        method: "GET",
      });
      await throwIfNotOk(response, "Failed to load user role memberships");
      return (await response.json()) as UserRoleMembership[];
    },
  });

export const clockDefinitionsQueryOptions = (tenantId?: string) =>
  queryOptions({
    queryKey: ["clock-definitions", tenantId ?? null],
    enabled: Boolean(tenantId),
    queryFn: async () => {
      if (!tenantId) {
        return [] as ClockDefinition[];
      }

      const params = new URLSearchParams();
      params.set("tenantId", tenantId);

      const response = await fetch(`/api/clock-definitions?${params}`, {
        method: "GET",
      });
      await throwIfNotOk(response, "Failed to load clock definitions");
      return (await response.json()) as ClockDefinition[];
    },
  });

export const recordClocksQueryOptions = (
  recordsetId?: string,
  recordId?: number,
) =>
  queryOptions({
    queryKey: ["record-clocks", recordsetId ?? null, recordId ?? null],
    enabled: Boolean(recordsetId) && Number.isInteger(recordId),
    queryFn: async () => {
      if (!recordsetId || !Number.isInteger(recordId)) {
        return [] as Clock[];
      }

      const response = await fetch(
        `/api/recordsets/${recordsetId}/records/${recordId}/clocks`,
        {
          method: "GET",
        },
      );
      await throwIfNotOk(response, "Failed to load record clocks");
      return (await response.json()) as Clock[];
    },
  });

export const notificationDetailsQueryOptions = (notificationId?: string) =>
  queryOptions({
    queryKey: ["notification", notificationId],
    queryFn: async () => {
      const response = await fetch(`/api/notifications/${notificationId}`, {
        method: "GET",
      });
      await throwIfNotOk(response, "Failed to load notification details");
      const raw = (await response.json()) as Record<string, unknown>;
      return normalizeNotificationDetails(raw);
    },
    enabled: Boolean(notificationId),
  });

export const unreadCountQueryOptions = (recordsetId?: string) =>
  queryOptions({
    queryKey: ["notifications-unread-count", recordsetId ?? null],
    queryFn: async () => {
      const url = recordsetId
        ? `/api/notifications/unreadCount?recordsetId=${encodeURIComponent(recordsetId)}`
        : "/api/notifications/unreadCount";
      const response = await fetch(url, { method: "GET" });
      await throwIfNotOk(response, "Failed to load unread notification count");
      return (await response.json()) as number;
    },
  });

export const notificationRulesQueryOptions = (recordsetId?: string) =>
  queryOptions({
    queryKey: ["notification-rules", recordsetId ?? null],
    enabled: Boolean(recordsetId),
    queryFn: async () => {
      if (!recordsetId) {
        return [] as NotificationRule[];
      }

      const params = new URLSearchParams();
      params.set("recordsetId", recordsetId);
      const request = new Request(
        `/api/notifications/rules?${params.toString()}`,
        { method: "GET" },
      );
      const response = await fetch(request);
      if (response.status === 204) {
        return [] as NotificationRule[];
      }
      await throwIfNotOk(response, "Failed to load notification rules");
      const raw = (await response.json()) as Record<string, unknown>[];
      return raw.map(normalizeNotificationRule);
    },
  });

interface MigrationJobStatusResponse {
  jobId: string;
  sourceRecordsetId: string;
  correlationId: string;
  stage: string;
  requestedBy?: string;
  createdOn?: string;
  startedOn?: string;
  completedOn?: string;
  backupExpiresOn?: string;
  backupRemovedOn?: string;
  attempts?: number;
  lastError?: string;
}

export const migrationProgressQueryOptions = (
  recordsetId: string,
  correlationId: string,
) =>
  queryOptions<MigrationProgressRecord | null>({
    queryKey: ["recordset-migration-progress", recordsetId, correlationId],
    enabled: Boolean(recordsetId) && Boolean(correlationId),
    queryFn: async () => {
      const response = await fetch(
        `/api/recordsets/${recordsetId}/migrations/${correlationId}`,
        { method: "GET" },
      );

      if (response.status === 404) {
        return null;
      }

      await throwIfNotOk(response, "Failed to load migration status");
      const dto = (await response.json()) as MigrationJobStatusResponse;
      const stage = dto.stage as MigrationProgressRecord["stage"];

      let percent: number | undefined;
      if (stage === "Completed" || stage === "Archived") {
        percent = 100;
      } else if (stage === "Failed") {
        percent = 0;
      }

      return {
        recordsetId: dto.sourceRecordsetId,
        correlationId: dto.correlationId,
        stage,
        requestedBy: dto.requestedBy,
        createdOn: dto.createdOn,
        startedOn: dto.startedOn,
        completedOn: dto.completedOn,
        backupExpiresOn: dto.backupExpiresOn,
        backupRemovedOn: dto.backupRemovedOn,
        attempts: dto.attempts,
        lastError: dto.lastError,
        updatedAt: dto.completedOn ?? dto.startedOn ?? dto.createdOn,
        percent,
      } satisfies MigrationProgressRecord;
    },
  });
