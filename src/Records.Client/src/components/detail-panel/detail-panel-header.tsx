import * as React from "react";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CloseIcon from "@mui/icons-material/Close";
import { Box, IconButton, Stack, Typography } from "@mui/material";

import useDetailPanel from "./use-detail-panel";

interface DetailPanelHeaderProps {
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  onBack?: () => void;
  backAriaLabel?: string;
  actions?: React.ReactNode;
}

const DetailPanelHeader = ({
  title: titleOverride,
  subtitle,
  onBack,
  backAriaLabel = "Back",
  actions,
}: DetailPanelHeaderProps) => {
  const { title, closeDetailPanel } = useDetailPanel();
  const displayTitle = titleOverride ?? title;

  const backButton = onBack ? (
    <IconButton onClick={onBack} aria-label={backAriaLabel}>
      <ArrowBackIcon />
    </IconButton>
  ) : null;

  const subtitleNode = subtitle ? (
    <Typography variant="body2" color="text.secondary">
      {subtitle}
    </Typography>
  ) : null;

  return (
    <Box
      sx={(theme) => ({
        display: "flex",
        flexDirection: "column",
        padding: theme.spacing(2),
        gap: subtitle ? theme.spacing(1) : 0,
        borderBottom: `1px solid ${theme.palette.divider}`,
      })}
    >
      <Stack direction="row" alignItems="center" spacing={1}>
        {backButton}
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          {displayTitle}
        </Typography>
        {actions}
        <IconButton onClick={closeDetailPanel} aria-label="Close">
          <CloseIcon />
        </IconButton>
      </Stack>
      {subtitleNode}
    </Box>
  );
};

export default DetailPanelHeader;
