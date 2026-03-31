import * as React from "react";

import MarkEmailReadIcon from "@mui/icons-material/MarkEmailRead";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  List,
  ListItemButton,
  Skeleton,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";
import {
  type InfiniteData,
  useInfiniteQuery,
  useMutation,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";

import {
  notificationDetailsQueryOptions,
  notificationsInfiniteQueryOptions,
  unreadCountQueryOptions,
} from "../../query-options";
import DetailPanelContainer from "../detail-panel/detail-panel-container";
import DetailPanelContent from "../detail-panel/detail-panel-content";
import DetailPanelFooter from "../detail-panel/detail-panel-footer";
import DetailPanelHeader from "../detail-panel/detail-panel-header";

import type {
  NotificationDetails,
  NotificationPage,
  NotificationsSearch,
  NotificationSummary,
} from "../../models/notifications";

const PAGE_SIZE = 20;

interface NotificationsDrawerProps {
  recordsetId?: string;
  contextLabel?: string;
}

const markNotificationRead = async (notificationId: string) => {
  const response = await fetch(`/api/notifications/${notificationId}/read`, {
    method: "POST",
  });
  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to mark notification as read");
    throw new Error(message);
  }
};

const markAllNotificationsRead = async (recordsetId?: string) => {
  const payload =
    typeof recordsetId === "string" && recordsetId.length > 0
      ? { recordsetId }
      : {};

  const response = await fetch(`/api/notifications/readAll`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    const message = await response
      .text()
      .catch(() => "Failed to mark notifications as read");
    throw new Error(message);
  }
};

const formatTimestamp = (isoString: string | undefined) => {
  if (!isoString) {
    return "";
  }
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) {
    return isoString;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
};

const buildSearch = (
  filter: "all" | "unread",
  recordsetId?: string,
): NotificationsSearch => ({
  unread: filter === "unread" ? true : undefined,
  recordsetId,
  pageSize: PAGE_SIZE,
});

const NOTIFICATION_SKELETON_KEYS = [
  "notification-skeleton-1",
  "notification-skeleton-2",
  "notification-skeleton-3",
  "notification-skeleton-4",
  "notification-skeleton-5",
] as const;

const NotificationListSkeleton = () => (
  <List
    disablePadding
    sx={{ display: "flex", flexDirection: "column", gap: 1.5, py: 0.5 }}
  >
    {NOTIFICATION_SKELETON_KEYS.map((skeletonKey) => (
      <ListItemButton
        key={skeletonKey}
        disabled
        sx={{
          alignItems: "flex-start",
          backgroundColor: "background.paper",
          borderColor: (theme) => theme.palette.divider,
        }}
      >
        <Stack spacing={1} sx={{ flexGrow: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} alignItems="center">
            <Skeleton variant="rounded" width={36} height={18} />
            <Skeleton variant="text" width="50%" />
            <Skeleton variant="text" width="18%" />
          </Stack>
          <Skeleton variant="text" width="95%" />
        </Stack>
        <Skeleton variant="circular" width={24} height={24} />
      </ListItemButton>
    ))}
  </List>
);

const NotificationsDrawer = ({
  recordsetId,
  contextLabel,
}: NotificationsDrawerProps) => {
  const queryClient = useQueryClient();
  const [activeNotificationId, setActiveNotificationId] = React.useState<
    string | null
  >(null);

  const invalidateNotifications = React.useCallback(() => {
    queryClient.invalidateQueries({
      queryKey: ["notifications"],
      exact: false,
    });
    queryClient.invalidateQueries({
      queryKey: ["notifications-unread-count"],
      exact: false,
    });
  }, [queryClient]);

  if (activeNotificationId) {
    return (
      <NotificationDetailsDrawer
        id={activeNotificationId}
        onBack={() => setActiveNotificationId(null)}
        invalidateNotifications={invalidateNotifications}
      />
    );
  }

  return (
    <NotificationsListDrawer
      onSelectNotification={setActiveNotificationId}
      invalidateNotifications={invalidateNotifications}
      recordsetId={recordsetId}
      contextLabel={contextLabel}
    />
  );
};

interface NotificationsListDrawerProps {
  onSelectNotification: (notificationId: string) => void;
  invalidateNotifications: () => void;
  recordsetId?: string;
  contextLabel?: string;
}

const NotificationsListDrawer = ({
  onSelectNotification,
  invalidateNotifications,
  recordsetId,
  contextLabel,
}: NotificationsListDrawerProps) => {
  const [filter, setFilter] = React.useState<"all" | "unread">("all");

  const { data: unreadTotal } = useSuspenseQuery(
    unreadCountQueryOptions(recordsetId),
  );
  const unreadCount = typeof unreadTotal === "number" ? unreadTotal : 0;
  const hasUnread = unreadCount > 0;

  const search = React.useMemo(
    () => buildSearch(filter, recordsetId),
    [filter, recordsetId],
  );

  const infiniteQuery = useInfiniteQuery(
    notificationsInfiniteQueryOptions(search),
  );

  const infiniteData = infiniteQuery.data as
    | InfiniteData<NotificationPage>
    | undefined;

  const pages = React.useMemo<NotificationPage[]>(() => {
    if (!infiniteData) {
      return [];
    }
    return [...infiniteData.pages];
  }, [infiniteData]);

  const notifications = React.useMemo<NotificationSummary[]>(() => {
    return pages.flatMap((page) => {
      if (!Array.isArray(page.notifications)) {
        return [] as NotificationSummary[];
      }

      return page.notifications.filter((item): item is NotificationSummary =>
        Boolean(item),
      );
    });
  }, [pages]);

  const totalAvailable = React.useMemo(() => {
    const firstPage = pages.length > 0 ? pages[0] : undefined;
    return firstPage?.totalCount ?? notifications.length;
  }, [pages, notifications.length]);

  const isInitialLoading =
    infiniteQuery.isPending ||
    (infiniteQuery.isFetching && notifications.length === 0);

  const markSingleMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => invalidateNotifications(),
  });

  const markAllMutation = useMutation({
    mutationFn: () => markAllNotificationsRead(recordsetId),
    onSuccess: () => invalidateNotifications(),
  });

  const handleMarkAll = React.useCallback(() => {
    const isMarkAllPending = markAllMutation.isPending;
    if (hasUnread && isMarkAllPending === false) {
      markAllMutation.mutate();
    }
  }, [hasUnread, markAllMutation]);

  const handleSelectFilter = React.useCallback(
    (_: React.SyntheticEvent, next: "all" | "unread" | null) => {
      if (next) {
        setFilter(next);
      }
    },
    [],
  );

  const handleLoadMore = React.useCallback(() => {
    const canLoadMore = Boolean(infiniteQuery.hasNextPage);
    const isLoadingNextPage = infiniteQuery.isFetchingNextPage === true;
    if (canLoadMore && isLoadingNextPage === false) {
      infiniteQuery.fetchNextPage();
    }
  }, [infiniteQuery]);

  const handleMarkSingle = React.useCallback(
    (event: React.MouseEvent, notificationId: string) => {
      event.stopPropagation();
      markSingleMutation.mutate(notificationId);
    },
    [markSingleMutation],
  );

  const handleOpenDetails = React.useCallback(
    (id: string) => {
      onSelectNotification(id);
    },
    [onSelectNotification],
  );

  const emptyStateMessage =
    filter === "unread" ? "You're all caught up" : "No notifications yet";

  const emptyStateBodyText = recordsetId
    ? "Notifications for this recordset will appear here."
    : "Notifications from your recordsets and records will appear here.";

  const emptyState = (
    <Box sx={{ py: 8, px: 2, textAlign: "center" }}>
      <NotificationsActiveIcon sx={{ fontSize: 48, color: "text.disabled" }} />
      <Typography variant="subtitle1" sx={{ mt: 2 }}>
        {emptyStateMessage}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {emptyStateBodyText}
      </Typography>
    </Box>
  );

  const isMarkingSingle = markSingleMutation.isPending;

  const notificationItems = notifications.map((notification) => {
    const isUnread = notification.isRead === false;
    const timestamp = formatTimestamp(notification.occurredAt);
    const timestampNode = timestamp ? (
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ ml: 1, whiteSpace: "nowrap" }}
      >
        {timestamp}
      </Typography>
    ) : undefined;

    const newBadge = isUnread ? (
      <Chip size="small" label="New" color="primary" variant="outlined" />
    ) : undefined;

    const titleWeight = isUnread ? 600 : 500;

    const markButton = isUnread ? (
      <Tooltip title="Mark as read">
        <IconButton
          edge="end"
          size="small"
          onClick={(event) => handleMarkSingle(event, notification.id)}
          disabled={isMarkingSingle}
          aria-label={`Mark "${notification.title}" as read`}
        >
          <MarkEmailReadIcon fontSize="small" />
        </IconButton>
      </Tooltip>
    ) : undefined;

    return (
      <ListItemButton
        key={notification.id}
        alignItems="flex-start"
        onClick={() => handleOpenDetails(notification.id)}
        sx={{
          backgroundColor: (theme) =>
            isUnread
              ? alpha(theme.palette.primary.main, 0.08)
              : theme.palette.background.paper,
          borderColor: (theme) =>
            isUnread ? theme.palette.primary.light : theme.palette.divider,
        }}
      >
        <Stack spacing={1} sx={{ flexGrow: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} alignItems="center">
            {newBadge}
            <Typography
              variant="subtitle2"
              fontWeight={titleWeight}
              noWrap
              sx={{ flexGrow: 1 }}
            >
              {notification.title}
            </Typography>
            {timestampNode}
          </Stack>
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {notification.body}
          </Typography>
        </Stack>
        {markButton}
      </ListItemButton>
    );
  });

  let content: React.ReactNode;
  if (isInitialLoading) {
    content = <NotificationListSkeleton />;
  } else if (notifications.length > 0) {
    content = (
      <List
        disablePadding
        sx={{ display: "flex", flexDirection: "column", gap: 1.5, py: 0.5 }}
      >
        {notificationItems}
      </List>
    );
  } else {
    content = emptyState;
  }

  const loadMoreLabel = infiniteQuery.hasNextPage
    ? "Load more"
    : "All caught up";

  const loadMoreIcon = infiniteQuery.isFetchingNextPage ? (
    <CircularProgress size={16} />
  ) : undefined;

  const hasMorePages = Boolean(infiniteQuery.hasNextPage);
  const loadMoreDisabled = isInitialLoading || hasMorePages === false;

  const unreadChipColor = hasUnread ? "primary" : "default";
  const unreadChipVariant = hasUnread ? "outlined" : "filled";
  const markAllDisabled = hasUnread ? markAllMutation.isPending : true;
  const markAllLabel = recordsetId ? "Mark recordset read" : "Mark all read";
  let headerSubtitle: string | undefined;
  if (recordsetId && contextLabel) {
    headerSubtitle = `Recordset: ${contextLabel}`;
  } else if (recordsetId) {
    headerSubtitle = "Showing notifications for this recordset.";
  }

  const footerContent = isInitialLoading ? (
    <Box
      sx={{
        display: "flex",
        width: "100%",
        justifyContent: "space-between",
        alignItems: "center",
        gap: 2,
      }}
    >
      <Skeleton variant="text" width={200} />
      <Skeleton variant="rounded" width={120} height={32} />
    </Box>
  ) : (
    <Box
      sx={{
        display: "flex",
        width: "100%",
        justifyContent: "space-between",
        alignItems: "center",
        gap: 2,
      }}
    >
      <Typography variant="body2" color="text.secondary">
        Showing {notifications.length} of {totalAvailable}
      </Typography>
      <Button
        variant="outlined"
        size="small"
        onClick={handleLoadMore}
        disabled={loadMoreDisabled}
        endIcon={loadMoreIcon}
      >
        {loadMoreLabel}
      </Button>
    </Box>
  );

  return (
    <DetailPanelContainer>
      <DetailPanelHeader
        subtitle={headerSubtitle}
        actions={
          <Stack direction="row" spacing={1} alignItems="center">
            <Chip
              icon={<NotificationsActiveIcon fontSize="small" />}
              label={`${unreadCount} unread`}
              size="small"
              color={unreadChipColor}
              variant={unreadChipVariant}
            />
            <Button
              size="small"
              variant="contained"
              startIcon={<MarkEmailReadIcon fontSize="small" />}
              onClick={handleMarkAll}
              disabled={markAllDisabled}
            >
              {markAllLabel}
            </Button>
          </Stack>
        }
      />
      <DetailPanelContent>
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            flex: 1,
            gap: 2,
            p: 2,
          }}
        >
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              flexWrap: "wrap",
              gap: 1,
            }}
          >
            <Typography variant="subtitle2">Filter notifications</Typography>
            <ToggleButtonGroup
              size="small"
              exclusive
              value={filter}
              onChange={handleSelectFilter}
              aria-label="Notification filter"
            >
              <ToggleButton value="all">All</ToggleButton>
              <ToggleButton value="unread">Unread</ToggleButton>
            </ToggleButtonGroup>
          </Box>
          <Box sx={{ flex: 1, minHeight: 0, overflowY: "auto" }}>{content}</Box>
        </Box>
      </DetailPanelContent>
      <DetailPanelFooter>{footerContent}</DetailPanelFooter>
    </DetailPanelContainer>
  );
};

interface NotificationDetailsDrawerProps {
  id: string;
  onBack: () => void;
  invalidateNotifications: () => void;
}

const NotificationDetailsDrawer = ({
  id,
  onBack,
  invalidateNotifications,
}: NotificationDetailsDrawerProps) => {
  const queryClient = useQueryClient();
  const detailsQueryOptions = React.useMemo(
    () => notificationDetailsQueryOptions(id),
    [id],
  );
  const { data } = useSuspenseQuery(detailsQueryOptions);

  const markReadMutation = useMutation({
    mutationFn: () => markNotificationRead(id),
    onSuccess: () => {
      invalidateNotifications();
      queryClient.setQueryData(
        detailsQueryOptions.queryKey,
        (previous: NotificationDetails | undefined) => {
          if (previous === undefined) {
            return previous;
          }
          return { ...previous, isRead: true };
        },
      );
    },
  });

  const metadataEntries = Object.entries(data.metadata ?? {});
  const historyEntries = data.history;
  const deliveryEntries = data.deliveryAttempts;
  const occurredAt =
    historyEntries.length > 0 ? historyEntries[0]?.occurredAt : undefined;
  const occurredAtLabel = formatTimestamp(occurredAt);

  const metadataSection =
    metadataEntries.length > 0 ? (
      <Box
        sx={{
          px: "1rem",
          py: "0.9rem",
          borderRadius: (theme) => theme.shape.borderRadius,
          border: (theme) => `1px solid ${theme.palette.divider}`,
          backgroundColor: (theme) => theme.palette.background.paper,
        }}
      >
        <Typography variant="subtitle2" gutterBottom>
          Metadata
        </Typography>
        <Box
          component="dl"
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "max-content 1fr" },
            columnGap: 2,
            rowGap: 1.5,
            m: 0,
          }}
        >
          {metadataEntries.map(([key, value]) => (
            <React.Fragment key={key}>
              <Typography
                component="dt"
                variant="body2"
                color="text.secondary"
                sx={{ minWidth: 96 }}
              >
                {key}
              </Typography>
              <Typography component="dd" variant="body2">
                {String(value)}
              </Typography>
            </React.Fragment>
          ))}
        </Box>
      </Box>
    ) : null;

  const historySection =
    historyEntries.length > 0 ? (
      <Box
        sx={{
          px: "1rem",
          py: "0.9rem",
          borderRadius: (theme) => theme.shape.borderRadius,
          border: (theme) => `1px solid ${theme.palette.divider}`,
          backgroundColor: (theme) => theme.palette.background.paper,
        }}
      >
        <Typography variant="subtitle2" gutterBottom>
          History
        </Typography>
        <Stack spacing={0.75}>
          {historyEntries.map((entry) => (
            <Typography
              key={`${entry.type}-${entry.occurredAt}`}
              variant="body2"
              color="text.secondary"
            >
              {formatTimestamp(entry.occurredAt)} — {entry.type}
            </Typography>
          ))}
        </Stack>
      </Box>
    ) : null;

  const deliverySection =
    deliveryEntries.length > 0 ? (
      <Box
        sx={{
          px: "1rem",
          py: "0.9rem",
          borderRadius: (theme) => theme.shape.borderRadius,
          border: (theme) => `1px solid ${theme.palette.divider}`,
          backgroundColor: (theme) => theme.palette.background.paper,
        }}
      >
        <Typography variant="subtitle2" gutterBottom>
          Delivery attempts
        </Typography>
        <Stack spacing={0.75}>
          {deliveryEntries.map((attempt) => {
            const failureSuffix = attempt.failureReason
              ? ` (${attempt.failureReason})`
              : "";

            const attemptStyle = attempt.failureReason
              ? {
                  px: "0.75rem",
                  py: "0.5rem",
                  borderRadius: (theme: any) => theme.shape.borderRadius,
                  border: (theme: any) =>
                    `1px solid ${theme.palette.error.light}`,
                  backgroundColor: (theme: any) =>
                    alpha(theme.palette.error.main, 0.04),
                }
              : undefined;

            return (
              <Typography
                key={`${attempt.channel}-${attempt.attemptNumber}`}
                variant="body2"
                color="text.secondary"
                sx={attemptStyle}
              >
                {formatTimestamp(attempt.attemptedAt)} — {attempt.channel} →{" "}
                {attempt.status}
                {failureSuffix}
              </Typography>
            );
          })}
        </Stack>
      </Box>
    ) : null;

  const markReadLabel = data.isRead ? "Already read" : "Mark as read";
  const markReadDisabled = data.isRead || markReadMutation.isPending;
  const markReadEndIcon = markReadMutation.isPending ? (
    <CircularProgress size={16} />
  ) : undefined;

  return (
    <DetailPanelContainer>
      <DetailPanelHeader
        title={data.title}
        subtitle={occurredAtLabel}
        onBack={onBack}
      />
      <DetailPanelContent>
        <Box
          sx={{
            p: 2,
            display: "flex",
            flexDirection: "column",
            gap: 2,
          }}
        >
          <Box
            sx={{
              p: 2,
              borderRadius: (theme) => theme.shape.borderRadius,
              border: (theme) => `1px solid ${theme.palette.divider}`,
              backgroundColor: (theme) => theme.palette.background.paper,
            }}
          >
            <Typography variant="body1">{data.body}</Typography>
          </Box>
          {deliverySection}
          {metadataSection}
          {historySection}
        </Box>
      </DetailPanelContent>
      <DetailPanelFooter>
        <Box
          sx={{
            display: "flex",
            width: "100%",
            justifyContent: "flex-end",
            alignItems: "center",
            gap: 2,
          }}
        >
          <Button
            variant="contained"
            startIcon={<MarkEmailReadIcon />}
            onClick={() => markReadMutation.mutate()}
            disabled={markReadDisabled}
            endIcon={markReadEndIcon}
          >
            {markReadLabel}
          </Button>
        </Box>
      </DetailPanelFooter>
    </DetailPanelContainer>
  );
};

export default NotificationsDrawer;
