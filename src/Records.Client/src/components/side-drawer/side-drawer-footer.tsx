import * as React from "react";
import { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type Props = PropsWithChildren;

const SideDrawerFooter = ({ children }: Props) => {
  return (
    <Box
      sx={(theme) => ({
        padding: theme.spacing(2),
        borderTop: `1px solid ${theme.palette.divider}`,
        backgroundColor: theme.palette.background.paper,
        display: "flex",
        width: "100%",
      })}
    >
      {children}
    </Box>
  );
};

export default SideDrawerFooter;
