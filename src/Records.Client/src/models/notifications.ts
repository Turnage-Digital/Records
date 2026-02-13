export type NotificationChannel =
  | "InApp"
  | "Email"
  | "Sms"
  | "Push"
  | "Webhook";

export type DeliveryStatus =
  | "Delivered"
  | "Failed"
  | "Pending"
  | "Processing"
  | "Skipped";

export interface DeliveryAttemptView {
  channel: NotificationChannel;
  // ISO timestamp
  attemptedOn: string;
  status: DeliveryStatus;
  failureReason?: string | null;
  attemptNumber: number;
}

export interface NotificationHistoryEntry {
  // e.g., Created | Delivered | Read
  type: string;
  // ISO timestamp
  on: string;
  by?: string | null;
  bag?: Record<string, unknown> | null;
}

export interface NotificationDetails {
  id: string;
  userId: string;
  recordsetId?: string | null;
  recordId?: number | null;
  title: string;
  body: string;
  metadata?: Record<string, unknown> | null;
  isRead: boolean;
  history: NotificationHistoryEntry[];
  deliveryAttempts: DeliveryAttemptView[];
}

export interface NotificationSummary {
  id: string;
  title: string;
  body: string;
  isRead: boolean;
  occurredOn: string;
  recordsetId?: string | null;
  recordId?: number | null;
  metadata?: Record<string, unknown> | null;
}

export interface NotificationPage {
  notifications: NotificationSummary[];
  totalCount: number;
  unreadCount: number;
  hasMore: boolean;
}

export interface NotificationsSearch {
  since?: string;
  unread?: boolean;
  recordsetId?: string;
  pageSize?: number;
  page?: number;
}
