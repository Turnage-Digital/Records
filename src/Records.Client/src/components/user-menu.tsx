import * as React from "react";

import { AccountCircle as AccountCircleIcon } from "@mui/icons-material";
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  Menu,
  MenuItem,
  Tooltip,
  Typography,
} from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";

import { useAuth } from "../auth";

const UserMenu = () => {
  const auth = useAuth();
  const queryClient = useQueryClient();
  const [userMenuAnchor, setUserMenuAnchor] =
    React.useState<null | HTMLElement>(null);
  const [logoutDialogOpen, setLogoutDialogOpen] = React.useState(false);

  const handleUserMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    setUserMenuAnchor(event.currentTarget);
  };

  const handleUserMenuClose = () => {
    setUserMenuAnchor(null);
  };

  const handleLogoutClick = () => {
    setUserMenuAnchor(null);
    setLogoutDialogOpen(true);
  };

  const handleLogoutConfirm = async () => {
    const request = new Request("/identity/logout", {
      method: "POST",
    });
    const response = await fetch(request);
    if (response.ok) {
      auth.logout();
      queryClient.clear();
    }
    setLogoutDialogOpen(false);
  };

  const handleLogoutCancel = () => {
    setLogoutDialogOpen(false);
  };

  return (
    <>
      <Tooltip title="Account">
        <IconButton color="primary" onClick={handleUserMenuClick}>
          <AccountCircleIcon />
        </IconButton>
      </Tooltip>

      <Menu
        anchorEl={userMenuAnchor}
        open={Boolean(userMenuAnchor)}
        onClose={handleUserMenuClose}
        anchorOrigin={{
          vertical: "bottom",
          horizontal: "right",
        }}
        transformOrigin={{
          vertical: "top",
          horizontal: "right",
        }}
      >
        <MenuItem disabled>
          <Typography variant="body2" color="text.secondary">
            {auth.username || "user@example.com"}
          </Typography>
        </MenuItem>
        <MenuItem divider />
        <MenuItem onClick={handleLogoutClick}>
          <Typography>Log Out</Typography>
        </MenuItem>
      </Menu>
      <Dialog
        open={logoutDialogOpen}
        onClose={handleLogoutCancel}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Log out</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to log out?
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleLogoutCancel}>Cancel</Button>
          <Button
            onClick={handleLogoutConfirm}
            variant="contained"
            color="primary"
          >
            Log Out
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default UserMenu;
