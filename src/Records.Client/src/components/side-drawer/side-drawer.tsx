import * as React from "react";

import {
  Box,
  CircularProgress,
  Dialog,
  Drawer,
  useMediaQuery,
  useTheme,
} from "@mui/material";

import useSideDrawer from "./use-side-drawer";

const SideDrawerFallback = () => (
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

const SideDrawer = () => {
  const { content, closeDrawer } = useSideDrawer();

  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.up("sm"));

  return fullScreen ? (
    <Drawer
      anchor="right"
      open={content !== null}
      onClose={closeDrawer}
      PaperProps={{
        sx: {
          width: 500,
          borderTopLeftRadius: 0,
          borderBottomLeftRadius: 0,
          overscrollBehavior: "contain",
        },
      }}
    >
      <React.Suspense fallback={<SideDrawerFallback />}>
        {content}
      </React.Suspense>
    </Drawer>
  ) : (
    <Dialog
      open={content !== null}
      onClose={closeDrawer}
      fullScreen
      PaperProps={{
        sx: {
          overscrollBehavior: "contain",
        },
      }}
    >
      <React.Suspense fallback={<SideDrawerFallback />}>
        {content}
      </React.Suspense>
    </Dialog>
  );
};

export default SideDrawer;
