import * as React from "react";
import type { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type DetailPanelFooterProps = PropsWithChildren;

const DetailPanelFooter = ({ children }: DetailPanelFooterProps) => {
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

export default DetailPanelFooter;
