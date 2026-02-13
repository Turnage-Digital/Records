import * as React from "react";

import { Paper, Stack } from "@mui/material";

interface AuthPageLayoutProps {
  children: React.ReactNode;
}

const AuthPageLayout = ({ children }: AuthPageLayoutProps) => (
  <Stack
    sx={{
      maxWidth: 440,
      mx: "auto",
      px: { xs: 2, sm: 0 },
      pt: 18,
      pb: 4,
    }}
  >
    <Paper
      elevation={1}
      sx={{
        p: { xs: 3, sm: 4 },
        borderRadius: (theme) => theme.shape.borderRadius,
      }}
    >
      <Stack spacing={4}>{children}</Stack>
    </Paper>
  </Stack>
);

export default AuthPageLayout;
