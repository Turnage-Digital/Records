import * as React from "react";
import { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type Props = PropsWithChildren;

const SideDrawerContainer = ({ children }: Props) => (
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

export default SideDrawerContainer;
