import * as React from "react";
import type { PropsWithChildren } from "react";

import { Box } from "@mui/material";

type DetailPanelContentProps = PropsWithChildren;

const DetailPanelContent = ({ children }: DetailPanelContentProps) => {
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

export default DetailPanelContent;
