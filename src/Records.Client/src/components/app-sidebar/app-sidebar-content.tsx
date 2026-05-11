import * as React from "react";

import AddCommentOutlinedIcon from "@mui/icons-material/AddCommentOutlined";
import ApartmentOutlinedIcon from "@mui/icons-material/ApartmentOutlined";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import DatasetOutlinedIcon from "@mui/icons-material/DatasetOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import ForumOutlinedIcon from "@mui/icons-material/ForumOutlined";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import {
  alpha,
  Box,
  Button,
  CircularProgress,
  Divider,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocation, useMatch, useNavigate } from "react-router-dom";

import { createAgentThread, deleteAgentThread } from "../../lib/agents";
import {
  agentThreadPath,
  agentsHomePath,
  recordsetsPath,
} from "../../lib/routes";
import { agentThreadSummariesQueryOptions } from "../../query-options";
import ConfirmDeleteDialog from "../confirm-delete-dialog";

import type { AgentThreadSummary } from "../../models/agent";

export interface AppSidebarContentProps {
  canManageGlobalAdminAreas: boolean;
  collapsed: boolean;
  isMobile: boolean;
  onCloseMobile: () => void;
  onToggleCollapse: () => void;
}

interface AppSidebarNavigationItem {
  label: string;
  icon: React.ReactNode;
  to: string;
  selected: boolean;
}

const AppSidebarContent = ({
  canManageGlobalAdminAreas,
  collapsed,
  isMobile,
  onCloseMobile,
  onToggleCollapse,
}: AppSidebarContentProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const threadMatch = useMatch("/threads/:threadId");
  const queryClient = useQueryClient();
  const threadsQuery = useQuery(agentThreadSummariesQueryOptions());
  const [threadToDelete, setThreadToDelete] =
    React.useState<AgentThreadSummary | null>(null);

  const createThreadMutation = useMutation({
    mutationFn: async () => createAgentThread(),
    onSuccess: async (thread) => {
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      navigate(agentThreadPath(thread.id));
      onCloseMobile();
    },
  });
  const deleteThreadMutation = useMutation({
    mutationFn: async (threadId: string) => deleteAgentThread(threadId),
    onSuccess: async (_result, deletedThreadId) => {
      queryClient.removeQueries({
        queryKey: ["agent-thread", deletedThreadId],
        exact: true,
      });
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });

      if (threadMatch?.params.threadId === deletedThreadId) {
        navigate(agentsHomePath());
      }

      onCloseMobile();
    },
  });

  const navigationItems: AppSidebarNavigationItem[] = [
    {
      label: "Agents",
      icon: <ForumOutlinedIcon />,
      to: agentsHomePath(),
      selected:
        location.pathname === "/" || location.pathname.startsWith("/threads/"),
    },
    {
      label: "Recordsets",
      icon: <DatasetOutlinedIcon />,
      to: recordsetsPath(),
      selected: location.pathname.startsWith("/recordsets"),
    },
  ];

  if (canManageGlobalAdminAreas) {
    navigationItems.push(
      {
        label: "Tenants",
        icon: <ApartmentOutlinedIcon />,
        to: "/admin/tenants",
        selected: location.pathname.startsWith("/admin/tenants"),
      },
      {
        label: "Users",
        icon: <GroupOutlinedIcon />,
        to: "/admin/users",
        selected: location.pathname.startsWith("/admin/users"),
      },
    );
  }

  const showExpandedContent = isMobile || !collapsed;
  const headerJustifyContent = showExpandedContent ? "space-between" : "center";
  const headerPaddingX = showExpandedContent ? 2 : 1;
  const navigationPaddingX = showExpandedContent ? 1.5 : 1;

  const handleNavigate = (to: string) => {
    navigate(to);
    onCloseMobile();
  };

  const handleCreateThread = () => {
    createThreadMutation.mutate();
  };

  const handleDeleteThreadClicked = (thread: AgentThreadSummary) => {
    setThreadToDelete(thread);
  };

  const handleConfirmDeleteThread = async () => {
    if (!threadToDelete) {
      return;
    }

    try {
      await deleteThreadMutation.mutateAsync(threadToDelete.id);
    } finally {
      setThreadToDelete(null);
    }
  };

  const handleCancelDeleteThread = () => {
    setThreadToDelete(null);
  };

  const renderNavigationItem = (item: AppSidebarNavigationItem) => {
    const itemText = showExpandedContent ? (
      <ListItemText
        primary={item.label}
        primaryTypographyProps={{ fontWeight: 600 }}
      />
    ) : null;

    const navigationButton = (
      <ListItemButton
        key={item.label}
        selected={item.selected}
        onClick={() => handleNavigate(item.to)}
        sx={{
          minHeight: 48,
          justifyContent: showExpandedContent ? "initial" : "center",
          px: showExpandedContent ? 1.5 : 1.25,
          borderRadius: 2,
          color: "inherit",
          "&.Mui-selected": {
            backgroundColor: alpha("#ffffff", 0.12),
          },
          "&.Mui-selected:hover": {
            backgroundColor: alpha("#ffffff", 0.18),
          },
        }}
      >
        <ListItemIcon
          sx={{
            minWidth: 0,
            mr: showExpandedContent ? 1.5 : 0,
            justifyContent: "center",
            color: "inherit",
          }}
        >
          {item.icon}
        </ListItemIcon>
        {itemText}
      </ListItemButton>
    );

    if (showExpandedContent) {
      return navigationButton;
    }

    return (
      <Tooltip key={item.label} title={item.label} placement="right">
        {navigationButton}
      </Tooltip>
    );
  };

  const deleteThreadDialogMessage = threadToDelete
    ? `Are you sure you want to delete "${threadToDelete.title}"? This action cannot be undone.`
    : "Are you sure you want to delete this thread? This action cannot be undone.";

  const renderThreadItem = (thread: AgentThreadSummary) => {
    const updatedAtLabel = new Date(thread.updatedAt).toLocaleString();

    return (
      <ListItem
        key={thread.id}
        disablePadding
        secondaryAction={
          <Tooltip title={`Delete ${thread.title}`}>
            <Box component="span">
              <IconButton
                edge="end"
                aria-label={`Delete ${thread.title}`}
                onClick={() => handleDeleteThreadClicked(thread)}
                disabled={deleteThreadMutation.isPending}
                sx={{
                  color: "rgba(255,255,255,0.66)",
                  "&:hover": { color: "common.white" },
                }}
              >
                <DeleteOutlineIcon fontSize="small" />
              </IconButton>
            </Box>
          </Tooltip>
        }
      >
        <ListItemButton
          selected={threadMatch?.params.threadId === thread.id}
          onClick={() => handleNavigate(agentThreadPath(thread.id))}
          sx={{
            borderRadius: 2,
            alignItems: "flex-start",
            px: 1.5,
            py: 1.2,
            pr: 6.5,
            "&.Mui-selected": {
              backgroundColor: alpha("#ffffff", 0.12),
            },
            "&.Mui-selected:hover": {
              backgroundColor: alpha("#ffffff", 0.18),
            },
          }}
        >
          <ListItemText
            primary={thread.title}
            secondary={updatedAtLabel}
            primaryTypographyProps={{
              fontWeight: 600,
              noWrap: true,
              color: "common.white",
            }}
            secondaryTypographyProps={{
              color: "rgba(255,255,255,0.66)",
              variant: "caption",
              noWrap: true,
            }}
          />
        </ListItemButton>
      </ListItem>
    );
  };

  const brandNode = showExpandedContent ? (
    <Box sx={{ minWidth: 0 }}>
      <Typography variant="subtitle2" sx={{ letterSpacing: "0.14em" }}>
        RECORDS
      </Typography>
      <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.66)" }}>
        Chat-first operations
      </Typography>
    </Box>
  ) : (
    <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
      R
    </Typography>
  );

  const headerNewThreadButton = showExpandedContent ? (
    <Tooltip title="New thread">
      <IconButton
        onClick={handleCreateThread}
        disabled={createThreadMutation.isPending}
        sx={{ color: "inherit" }}
      >
        <AddCommentOutlinedIcon />
      </IconButton>
    </Tooltip>
  ) : null;

  let collapseTooltipTitle = "Close";
  let collapseIcon: React.ReactNode = <ChevronLeftIcon />;
  let collapseHandler = onCloseMobile;

  if (!isMobile) {
    collapseHandler = onToggleCollapse;
    collapseTooltipTitle = collapsed ? "Expand" : "Collapse";
    collapseIcon = collapsed ? <ChevronRightIcon /> : <ChevronLeftIcon />;
  }

  const compactNewThreadButton = showExpandedContent ? null : (
    <Tooltip title="New thread" placement="right">
      <ListItemButton
        onClick={handleCreateThread}
        disabled={createThreadMutation.isPending}
        sx={{
          minHeight: 48,
          justifyContent: "center",
          borderRadius: 2,
          mb: 1,
        }}
      >
        <ListItemIcon
          sx={{
            minWidth: 0,
            justifyContent: "center",
            color: "inherit",
          }}
        >
          <AddCommentOutlinedIcon />
        </ListItemIcon>
      </ListItemButton>
    </Tooltip>
  );

  const pendingIndicator = createThreadMutation.isPending ? (
    <CircularProgress size={14} sx={{ color: "inherit" }} />
  ) : null;

  const loadingThreadsNotice = threadsQuery.isLoading ? (
    <Box sx={{ px: 1.5, py: 2 }}>
      <Typography variant="body2" color="rgba(255,255,255,0.66)">
        Loading recent conversations...
      </Typography>
    </Box>
  ) : null;

  const hasNoThreads =
    !threadsQuery.isLoading && (threadsQuery.data?.length ?? 0) === 0;
  const emptyThreadsNotice = hasNoThreads ? (
    <Box sx={{ px: 1.5, py: 2 }}>
      <Typography variant="body2" color="rgba(255,255,255,0.66)">
        Start a thread to keep working conversations close at hand.
      </Typography>
    </Box>
  ) : null;

  const recentThreadsSection = showExpandedContent ? (
    <>
      <Divider sx={{ borderColor: "rgba(255,255,255,0.08)" }} />
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: "flex",
          flexDirection: "column",
          px: 1.5,
          py: 1.5,
        }}
      >
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ px: 1.5, pb: 1 }}
        >
          <Typography
            variant="caption"
            sx={{
              color: "rgba(255,255,255,0.66)",
              letterSpacing: "0.08em",
            }}
          >
            RECENT THREADS
          </Typography>
          {pendingIndicator}
        </Stack>

        <List
          sx={{
            p: 0,
            overflow: "auto",
            flex: 1,
          }}
        >
          {loadingThreadsNotice}
          {threadsQuery.data?.map((thread) => renderThreadItem(thread))}
          {emptyThreadsNotice}
        </List>

        <Button
          variant="outlined"
          startIcon={<AddCommentOutlinedIcon />}
          onClick={handleCreateThread}
          disabled={createThreadMutation.isPending}
          sx={{
            mt: 1.5,
            borderColor: "rgba(255,255,255,0.18)",
            color: "inherit",
          }}
        >
          New thread
        </Button>
      </Box>
    </>
  ) : null;

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
      }}
    >
      <Stack
        direction="row"
        alignItems="center"
        justifyContent={headerJustifyContent}
        spacing={1}
        sx={{ px: headerPaddingX, py: 2 }}
      >
        {brandNode}

        <Stack direction="row" spacing={0.5}>
          {headerNewThreadButton}

          <Tooltip title={collapseTooltipTitle}>
            <IconButton onClick={collapseHandler} sx={{ color: "inherit" }}>
              {collapseIcon}
            </IconButton>
          </Tooltip>
        </Stack>
      </Stack>

      <Divider sx={{ borderColor: "rgba(255,255,255,0.08)" }} />

      <Box sx={{ px: navigationPaddingX, py: 1.5 }}>
        {compactNewThreadButton}
        <List sx={{ p: 0 }}>{navigationItems.map(renderNavigationItem)}</List>
      </Box>

      {recentThreadsSection}

      <ConfirmDeleteDialog
        open={Boolean(threadToDelete)}
        title="Delete thread"
        description={deleteThreadDialogMessage}
        confirmDisabled={deleteThreadMutation.isPending}
        onCancel={handleCancelDeleteThread}
        onConfirm={handleConfirmDeleteThread}
      />
    </Box>
  );
};

export default AppSidebarContent;
