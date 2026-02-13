import * as React from "react";
import { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type Props = PropsWithChildren;

const SideDrawerContent = ({ children }: Props) => {
  return (
    <Box
      sx={{
        display: "flex",
        flex: 1,
        flexDirection: "column",
        width: "100%",
        overflowY: "auto",
      }}
    >
      {children}
    </Box>
  );
};

export default SideDrawerContent;
