import * as React from "react";

import { Dialog, Drawer, useMediaQuery, useTheme } from "@mui/material";

import useSideDrawer from "./use-side-drawer";

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
        },
      }}
    >
      {content}
    </Drawer>
  ) : (
    <Dialog open={content !== null} onClose={closeDrawer} fullScreen>
      {content}
    </Dialog>
  );
};

export default SideDrawer;
