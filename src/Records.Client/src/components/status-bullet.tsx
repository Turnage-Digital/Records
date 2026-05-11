import * as React from "react";

import { Box } from "@mui/material";

import type { StatusColor } from "../models/status-colors";

interface StatusBulletProps {
  statusColor: StatusColor;
}

const StatusBullet = ({ statusColor }: StatusBulletProps) => {
  return (
    <Box
      component="span"
      sx={(theme) => ({
        borderRadius: "50%",
        height: theme.spacing(2),
        width: theme.spacing(2),
        backgroundColor: statusColor.value,
      })}
    />
  );
};

export default StatusBullet;
