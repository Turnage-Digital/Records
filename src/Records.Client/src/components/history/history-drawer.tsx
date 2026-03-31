import * as React from "react";

import RestoreIcon from "@mui/icons-material/Restore";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Skeleton,
  Stack,
  Typography,
} from "@mui/material";
import {
  type InfiniteData,
  type UseInfiniteQueryResult,
} from "@tanstack/react-query";

import DetailPanelContainer from "../detail-panel/detail-panel-container";
import DetailPanelContent from "../detail-panel/detail-panel-content";
import DetailPanelFooter from "../detail-panel/detail-panel-footer";
import DetailPanelHeader from "../detail-panel/detail-panel-header";

import type { HistoryPage } from "../../models/history";

const formatTimestamp = (isoString: string | undefined) => {
  if (isoString === undefined || isoString === "") {
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

interface HistoryDrawerProps {
  subtitle?: string;
  query: UseInfiniteQueryResult<HistoryPage>;
}

const HISTORY_SKELETON_KEYS = [
  "history-skeleton-1",
  "history-skeleton-2",
  "history-skeleton-3",
  "history-skeleton-4",
] as const;

const HistorySkeleton = () => (
  <Stack sx={{ flex: 1 }}>
    {HISTORY_SKELETON_KEYS.map((skeletonKey, index) => {
      const hasSkeletonConnector = index < HISTORY_SKELETON_KEYS.length - 1;

      const connector = hasSkeletonConnector ? (
        <Box
          sx={{
            flex: 1,
            width: 2,
            backgroundColor: (theme) => theme.palette.divider,
            mt: 1,
          }}
        />
      ) : null;

      return (
        <Box key={skeletonKey} sx={{ py: 1, alignItems: "stretch" }}>
          <Stack direction="row" spacing={2} sx={{ width: "100%" }}>
            <Box
              sx={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                minWidth: 32,
              }}
            >
              <Skeleton variant="circular" width={32} height={32} />
              {connector}
            </Box>
            <Stack
              spacing={1}
              sx={{
                flex: 1,
                px: "1rem",
                py: "0.9rem",
                borderRadius: (theme) => theme.shape.borderRadius,
                border: (theme) => `1px solid ${theme.palette.divider}`,
                backgroundColor: (theme) => theme.palette.background.paper,
              }}
            >
              <Skeleton variant="text" width="35%" />
              <Skeleton variant="text" width="55%" />
              <Skeleton variant="text" width="25%" />
            </Stack>
          </Stack>
        </Box>
      );
    })}
  </Stack>
);

const HistoryDrawer = ({ subtitle, query }: HistoryDrawerProps) => {
  const infiniteData = query.data as InfiniteData<HistoryPage> | undefined;

  const pages = React.useMemo<HistoryPage[]>(() => {
    if (infiniteData === undefined) {
      return [];
    }
    return [...infiniteData.pages];
  }, [infiniteData]);

  const entries = React.useMemo(() => {
    return pages.flatMap((page) => page.items);
  }, [pages]);

  const totalEntries = pages.length > 0 ? pages[0]?.total : undefined;

  const isInitialLoading =
    query.isPending || (query.isFetching && entries.length === 0);

  const loadMoreIcon = query.isFetchingNextPage ? (
    <CircularProgress size={16} />
  ) : undefined;

  let loadMoreNode: React.ReactNode;
  if (isInitialLoading) {
    loadMoreNode = <CircularProgress size={20} />;
  } else if (query.hasNextPage) {
    loadMoreNode = (
      <Button
        variant="outlined"
        size="small"
        onClick={() => query.fetchNextPage()}
        endIcon={loadMoreIcon}
        disabled={query.isFetchingNextPage}
      >
        Load more
      </Button>
    );
  } else {
    loadMoreNode = (
      <Typography variant="caption" color="text.secondary">
        All history loaded
      </Typography>
    );
  }

  const historyContent =
    entries.length > 0 ? (
      <Stack sx={{ flex: 1 }}>
        {entries.map((entry, index) => {
          const entryKey = `${entry.occurredAt}-${entry.type}-${entry.actorId ?? "unknown"}`;
          const hasTrailingConnector = index < entries.length - 1;

          const performerLine = entry.actorId ? (
            <Typography variant="body2" color="text.secondary">
              Performed by {entry.actorId}
            </Typography>
          ) : null;

          const entryConnector = hasTrailingConnector ? (
            <Box
              sx={{
                flex: 1,
                width: 2,
                backgroundColor: (theme) => theme.palette.divider,
                mt: 1,
                borderRadius: (theme) => theme.shape.borderRadius,
              }}
            />
          ) : null;

          return (
            <Box key={entryKey} sx={{ py: 1, alignItems: "stretch" }}>
              <Stack direction="row" spacing={2} sx={{ width: "100%" }}>
                <Box
                  sx={{
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "center",
                    minWidth: 32,
                  }}
                >
                  <Box
                    sx={{
                      width: 32,
                      height: 32,
                      borderRadius: "50%",
                      backgroundColor: "primary.main",
                      color: "primary.contrastText",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                    }}
                  >
                    <RestoreIcon fontSize="small" />
                  </Box>
                  {entryConnector}
                </Box>
                <Stack
                  spacing={0.5}
                  sx={{
                    flex: 1,
                    px: "1rem",
                    py: "0.9rem",
                    borderRadius: (theme) => theme.shape.borderRadius,
                    border: (theme) => `1px solid ${theme.palette.divider}`,
                    backgroundColor: (theme) => theme.palette.background.paper,
                  }}
                >
                  <Typography variant="caption" color="text.secondary">
                    {formatTimestamp(entry.occurredAt)}
                  </Typography>
                  <Typography variant="subtitle2">{entry.type}</Typography>
                  {performerLine}
                </Stack>
              </Stack>
            </Box>
          );
        })}
      </Stack>
    ) : (
      <Box
        sx={{
          py: 8,
          textAlign: "center",
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: 2,
        }}
      >
        <Box
          sx={{
            width: 56,
            height: 56,
            borderRadius: "50%",
            backgroundColor: (theme) => theme.palette.grey[100],
            color: (theme) => theme.palette.primary.dark,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <RestoreIcon />
        </Box>
        <Typography variant="subtitle1">No history yet</Typography>
        <Typography variant="body2" color="text.secondary">
          Activity on your recordsets will start appearing here.
        </Typography>
      </Box>
    );

  const contentNode = isInitialLoading ? <HistorySkeleton /> : historyContent;

  let headerActions: React.ReactNode;
  if (typeof totalEntries === "number") {
    headerActions = (
      <Chip
        size="small"
        label={`${totalEntries} entr${totalEntries === 1 ? "y" : "ies"}`}
        color="default"
      />
    );
  }

  const hasNextPage = Boolean(query.hasNextPage);
  let footerJustify: "center" | "flex-end" = "center";
  if (hasNextPage) {
    footerJustify = isInitialLoading ? "center" : "flex-end";
  }

  return (
    <DetailPanelContainer>
      <DetailPanelHeader subtitle={subtitle} actions={headerActions} />
      <DetailPanelContent>
        <Box
          sx={{
            display: "flex",
            flexDirection: "column",
            flex: 1,
            p: 2,
            gap: 2,
          }}
        >
          <Box
            sx={{
              flex: 1,
              overflowY: "auto",
              display: "flex",
              flexDirection: "column",
            }}
          >
            {contentNode}
          </Box>
        </Box>
      </DetailPanelContent>
      <DetailPanelFooter>
        <Box
          sx={{
            display: "flex",
            width: "100%",
            justifyContent: footerJustify,
          }}
        >
          {loadMoreNode}
        </Box>
      </DetailPanelFooter>
    </DetailPanelContainer>
  );
};

export default HistoryDrawer;
