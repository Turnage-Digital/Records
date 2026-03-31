import * as React from "react";
import type { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type DetailPanelContainerProps = PropsWithChildren;

const DetailPanelContainer = ({ children }: DetailPanelContainerProps) => (
  <Box
    sx={{
      display: "flex",
      flexDirection: "column",
      minHeight: "100%",
      height: "100%",
      width: "100%",
      backgroundColor: (theme) => theme.palette.background.paper,
    }}
  >
    {children}
  </Box>
);

export default DetailPanelContainer;
