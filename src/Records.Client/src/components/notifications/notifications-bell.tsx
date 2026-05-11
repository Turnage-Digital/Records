import * as React from "react";

import NotificationsIcon from "@mui/icons-material/Notifications";
import { Badge, IconButton, Tooltip } from "@mui/material";
import { useSuspenseQuery } from "@tanstack/react-query";

import { unreadCountQueryOptions } from "../../query-options";
import useDetailPanel from "../detail-panel/use-detail-panel";

const NotificationsDrawer = React.lazy(() => import("./notifications-drawer"));

const NotificationsBell = () => {
  const { data: unreadCount } = useSuspenseQuery(unreadCountQueryOptions());
  const { openDetailPanel } = useDetailPanel();

  const onClick = (): void => {
    openDetailPanel("Notifications", <NotificationsDrawer />);
  };

  return (
    <Tooltip title="Notifications">
      <IconButton
        color="primary"
        onClick={onClick}
        aria-label="Open notifications"
      >
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
