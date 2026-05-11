import * as React from "react";

import { Button, Stack, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";

import { useAuth } from "../auth";
import AuthPageLayout from "../components/auth-page-layout";

const AccessDeniedPage = () => {
  const auth = useAuth();
  const navigate = useNavigate();
  const signedInLabel = auth.username
    ? `Signed in as ${auth.username}.`
    : "Try another account or ask an administrator to grant access.";

  const handleLogout = async () => {
    const response = await fetch("/identity/logout", {
      method: "POST",
    });

    if (!response.ok) {
      return;
    }

    await auth.logout();
    navigate("/sign-in", { replace: true });
  };

  return (
    <AuthPageLayout>
      <Stack spacing={3}>
        <Stack spacing={1} textAlign="center">
          <Typography variant="h5" component="h1">
            Access required
          </Typography>
          <Typography color="text.secondary">
            This account is signed in, but it does not have Records operations
            access.
          </Typography>
          <Typography color="text.secondary">{signedInLabel}</Typography>
        </Stack>

        <Button variant="contained" size="large" onClick={handleLogout}>
          Sign in with a different account
        </Button>
      </Stack>
    </AuthPageLayout>
  );
};

export default AccessDeniedPage;
