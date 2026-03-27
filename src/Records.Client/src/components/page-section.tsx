import * as React from "react";

import { Box, Paper, Stack, Typography } from "@mui/material";

export interface PageSectionProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;
  children: React.ReactNode;
}

const PageSection = ({
  title,
  description,
  actions,
  children,
}: PageSectionProps) => {
  const descriptionNode = description ? (
    <Typography variant="body2" color="text.secondary" sx={{ mt: 0.75 }}>
      {description}
    </Typography>
  ) : null;

  const actionsNode = actions ? <Box>{actions}</Box> : null;

  return (
    <Paper elevation={1} sx={{ p: { xs: 2.5, md: 3 } }}>
      <Stack spacing={2.5}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          alignItems={{ xs: "stretch", md: "flex-start" }}
        >
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6">{title}</Typography>
            {descriptionNode}
          </Box>
          {actionsNode}
        </Stack>

        <Box sx={{ minWidth: 0 }}>{children}</Box>
      </Stack>
    </Paper>
  );
};

export default PageSection;
