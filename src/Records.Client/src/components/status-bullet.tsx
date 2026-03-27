import * as React from "react";

import { Box } from "@mui/material";

import { StatusColor } from "../models";

interface Props {
  statusColor: StatusColor;
}

const StatusBullet = ({ statusColor }: Props) => {
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
