import * as React from "react";

import { Box, Drawer } from "@mui/material";

import AppSidebarContent from "./app-sidebar-content";

export interface AppSidebarProps {
  canManageGlobalAdminAreas: boolean;
  collapsed: boolean;
  mobileOpen: boolean;
  onCloseMobile: () => void;
  onToggleCollapse: () => void;
}

const expandedSidebarWidth = 320;
const collapsedSidebarWidth = 88;

const AppSidebar = ({
  canManageGlobalAdminAreas,
  collapsed,
  mobileOpen,
  onCloseMobile,
  onToggleCollapse,
}: AppSidebarProps) => {
  return (
    <>
      <Box
        sx={{
          display: { xs: "none", lg: "block" },
          width: collapsed ? collapsedSidebarWidth : expandedSidebarWidth,
          flexShrink: 0,
        }}
      >
        <Drawer
          variant="permanent"
          open
          PaperProps={{
            sx: {
              width: collapsed ? collapsedSidebarWidth : expandedSidebarWidth,
              borderRight: 1,
              borderColor: "divider",
              background:
                "linear-gradient(180deg, #14181d 0%, #1b2128 55%, #1f2730 100%)",
              color: "common.white",
              overflowX: "hidden",
            },
          }}
        >
          <AppSidebarContent
            canManageGlobalAdminAreas={canManageGlobalAdminAreas}
            collapsed={collapsed}
            isMobile={false}
            onCloseMobile={onCloseMobile}
            onToggleCollapse={onToggleCollapse}
          />
        </Drawer>
      </Box>

      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onCloseMobile}
        ModalProps={{ keepMounted: true }}
        PaperProps={{
          sx: {
            width: expandedSidebarWidth,
            borderRight: 1,
            borderColor: "divider",
            background:
              "linear-gradient(180deg, #14181d 0%, #1b2128 55%, #1f2730 100%)",
            color: "common.white",
          },
        }}
        sx={{ display: { xs: "block", lg: "none" } }}
      >
        <AppSidebarContent
          canManageGlobalAdminAreas={canManageGlobalAdminAreas}
          collapsed={false}
          isMobile
          onCloseMobile={onCloseMobile}
          onToggleCollapse={onToggleCollapse}
        />
      </Drawer>
    </>
  );
};

export default AppSidebar;
