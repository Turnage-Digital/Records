import * as React from "react";

import { ChevronRight } from "@mui/icons-material";
import {
  Box,
  Breadcrumbs,
  Button,
  type ButtonProps,
  Grid,
  Link,
  Stack,
  Typography,
} from "@mui/material";

export interface Action {
  title: string;
  icon?: React.ReactNode;
  onClick?: () => void;
  variant?: "contained" | "outlined";
  color?: ButtonProps["color"];
}

export interface Breadcrumb {
  title: string;
  onClick?: () => void;
}

export interface TitlebarProps {
  title: string;
  actions?: Action[];
  breadcrumbs?: Breadcrumb[];
}

const Titlebar = ({ title, actions, breadcrumbs }: TitlebarProps) => {
  const hasActions = Boolean(actions && actions.length > 0);
  const titleGridSizeMd = hasActions ? 8 : 12;
  const actionsNode = hasActions ? (
    <Grid
      size={{ xs: 12, md: 4 }}
      sx={{
        display: "flex",
        justifyContent: { xs: "flex-start", md: "flex-end" },
      }}
    >
      <Stack direction="row" spacing={1.5}>
        {actions!.map((action, index) => {
          const variant =
            action.variant ?? (index === 0 ? "contained" : "outlined");
          const color = action.color ?? "primary";
          return (
            <Button
              key={action.title}
              variant={variant}
              startIcon={action.icon}
              onClick={action.onClick}
              color={color}
            >
              {action.title}
            </Button>
          );
        })}
      </Stack>
    </Grid>
  ) : null;

  const hasBreadcrumbs = Boolean(breadcrumbs && breadcrumbs.length > 0);
  const breadcrumbsNode = hasBreadcrumbs ? (
    <Box>
      <Breadcrumbs
        separator={
          <ChevronRight sx={{ fontSize: 16, color: "text.secondary" }} />
        }
      >
        {breadcrumbs!.map((breadcrumb) => (
          <Link
            key={breadcrumb.title}
            underline="none"
            onClick={breadcrumb.onClick}
            sx={{
              cursor: "pointer",
              fontSize: "0.875rem",
              fontWeight: 500,
              color: "text.secondary",
              transition: "color 0.2s ease-in-out",
              "&:hover": {
                color: "text.primary",
              },
            }}
          >
            {breadcrumb.title}
          </Link>
        ))}
        <Typography
          sx={{
            fontSize: "0.875rem",
            fontWeight: 500,
            color: "text.primary",
          }}
        >
          {title}
        </Typography>
      </Breadcrumbs>
    </Box>
  ) : null;

  return (
    <Box sx={{ mb: { xs: 3, md: 4 } }}>
      {/* Title and Actions Section */}
      <Grid
        container
        alignItems="center"
        spacing={{ xs: 2, md: 3 }}
        sx={{ mb: { xs: 2, md: 3 } }}
      >
        <Grid size={{ xs: 12, md: titleGridSizeMd }}>
          <Typography
            variant="h4"
            component="h1"
            sx={{
              fontWeight: 700,
              fontSize: { xs: "1.75rem", md: "2.125rem" },
              lineHeight: 1.2,
            }}
          >
            {title}
          </Typography>
        </Grid>

        {actionsNode}
      </Grid>

      {/* Breadcrumbs Section */}
      {breadcrumbsNode}
    </Box>
  );
};

export default Titlebar;
