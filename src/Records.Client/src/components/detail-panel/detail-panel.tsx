import * as React from "react";

import {
  Box,
  CircularProgress,
  Dialog,
  Drawer,
  useMediaQuery,
  useTheme,
} from "@mui/material";

import useDetailPanel from "./use-detail-panel";

const DetailPanelFallback = () => (
  <Box
    role="status"
    aria-live="polite"
    sx={{
      display: "flex",
      justifyContent: "center",
      alignItems: "center",
      minHeight: 240,
      px: 3,
    }}
  >
    <CircularProgress />
  </Box>
);

const DetailPanel = () => {
  const { content, closeDetailPanel } = useDetailPanel();

  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.up("sm"));

  return fullScreen ? (
    <Drawer
      anchor="right"
      open={content !== null}
      onClose={closeDetailPanel}
      PaperProps={{
        sx: {
          width: 500,
          borderTopLeftRadius: 0,
          borderBottomLeftRadius: 0,
          overscrollBehavior: "contain",
        },
      }}
    >
      <React.Suspense fallback={<DetailPanelFallback />}>
        {content}
      </React.Suspense>
    </Drawer>
  ) : (
    <Dialog
      open={content !== null}
      onClose={closeDetailPanel}
      fullScreen
      PaperProps={{
        sx: {
          overscrollBehavior: "contain",
        },
      }}
    >
      <React.Suspense fallback={<DetailPanelFallback />}>
        {content}
      </React.Suspense>
    </Dialog>
  );
};

export default DetailPanel;
