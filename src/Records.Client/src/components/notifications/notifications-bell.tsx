import * as React from "react";

import NotificationsIcon from "@mui/icons-material/Notifications";
import { Badge, IconButton, Tooltip } from "@mui/material";
import { useSuspenseQuery } from "@tanstack/react-query";

import NotificationsDrawer from "./notifications-drawer";
import { unreadCountQueryOptions } from "../../query-options";
import useSideDrawer from "../side-drawer/use-side-drawer";

const NotificationsBell = () => {
  const { data: unreadCount } = useSuspenseQuery(unreadCountQueryOptions());
  const { openDrawer } = useSideDrawer();

  const onClick = (): void => {
    openDrawer("Notifications", <NotificationsDrawer />);
  };

  return (
    <Tooltip title="Notifications">
      <IconButton color="primary" onClick={onClick}>
        <Badge
          color="error"
          badgeContent={unreadCount}
          max={99}
          overlap="circular"
        >
          <NotificationsIcon />
        </Badge>
      </IconButton>
    </Tooltip>
  );
};

export default NotificationsBell;
