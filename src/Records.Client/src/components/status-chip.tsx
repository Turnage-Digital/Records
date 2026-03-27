import * as React from "react";

import {
  Cancel,
  CheckCircle,
  Flag,
  RadioButtonUnchecked,
  Schedule,
} from "@mui/icons-material";
import { alpha, Chip, darken } from "@mui/material";

import { Status } from "../models";

interface Props {
  status?: Status;
  onDelete?: () => void;
}

const StatusChip = ({ status, onDelete }: Props) => {
  // Get appropriate icon based on status name (you can customize this logic)
  const getStatusIcon = (statusName: string) => {
    const name = statusName.toLowerCase();
    if (
      name.includes("complete") ||
      name.includes("done") ||
      name.includes("finished")
    ) {
      return <CheckCircle sx={{ fontSize: 16 }} />;
    }
    if (
      name.includes("progress") ||
      name.includes("pending") ||
      name.includes("active")
    ) {
      return <Schedule sx={{ fontSize: 16 }} />;
    }
    if (
      name.includes("cancel") ||
      name.includes("reject") ||
      name.includes("failed")
    ) {
      return <Cancel sx={{ fontSize: 16 }} />;
    }
    if (
      name.includes("new") ||
      name.includes("open") ||
      name.includes("todo")
    ) {
      return <RadioButtonUnchecked sx={{ fontSize: 16 }} />;
    }
    return <Flag sx={{ fontSize: 16 }} />;
  };

  if (!status) {
    return null;
  }

  return (
    <Chip
      icon={getStatusIcon(status.name)}
      label={status.name}
      size="small"
      sx={{
        color: darken(status.color, 0.55),
        fontWeight: 600,
        fontSize: "0.75rem",
        backgroundColor: alpha(status.color, 0.12),
        border: `1px solid ${alpha(status.color, 0.28)}`,
        transition: "background-color 0.2s ease, color 0.2s ease",
        "& .MuiChip-icon": {
          color: darken(status.color, 0.4),
          transition: "color 0.2s ease",
        },
        "&:hover": {
          backgroundColor: alpha(status.color, 0.16),
        },
      }}
      onDelete={onDelete}
    />
  );
};

export default StatusChip;
