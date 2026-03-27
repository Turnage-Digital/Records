import * as React from "react";
import { ReactNode } from "react";

import { Grid, Stack, Typography } from "@mui/material";

interface Props {
  title: string;
  subtitle?: string;
  content: ReactNode;
}

const FormBlock = ({ title, subtitle, content }: Props) => {
  return (
    <Grid
      container
      columnSpacing={{ xs: 5, md: 8 }}
      rowSpacing={{ xs: 4.5, md: 5.5 }}
      alignItems="flex-start"
    >
      <Grid size={{ xs: 12, md: 4 }}>
        <Stack spacing={1.5} sx={{ pb: { xs: 2, md: 1 } }}>
          <Typography color="text.primary" variant="h6" fontWeight={600}>
            {title}
          </Typography>
          {subtitle && (
            <Typography variant="body2" color="text.secondary">
              {subtitle}
            </Typography>
          )}
        </Stack>
      </Grid>
      <Grid size={{ xs: 12, md: 8 }}>{content}</Grid>
    </Grid>
  );
};

export default FormBlock;
