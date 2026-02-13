import * as React from "react";

import { Alert, Link, Stack, Typography } from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";

import { AuthPageLayout, ResetPasswordForm } from "../components";

const ResetPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const email = searchParams.get("email") ?? undefined;
  const resetCode = searchParams.get("code") ?? undefined;

  React.useEffect(() => {
    if (!email || !resetCode) {
      setErrorMessage(
        "Invalid reset link. Please request a new password reset.",
      );
    }
  }, [email, resetCode]);

  const handleNavigateToSignIn = () => {
    navigate("/sign-in");
  };

  const formContent =
    email && resetCode ? (
      <ResetPasswordForm
        email={email}
        resetCode={resetCode}
        onPasswordReset={handleNavigateToSignIn}
      />
    ) : null;

  return (
    <AuthPageLayout>
      <Stack spacing={2} alignItems="center">
        <Typography variant="h5" align="center" gutterBottom>
          Reset Password
        </Typography>
        <Typography variant="body2" color="text.secondary" align="center">
          Choose a new password to get back into your account.
        </Typography>
      </Stack>

      {formContent}

      {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

      <Stack spacing={2} alignItems="center">
        <Typography
          variant="body2"
          color="text.secondary"
          align="center"
          component="div"
        >
          Already have an account?{" "}
          <Link
            component="button"
            type="button"
            onClick={handleNavigateToSignIn}
          >
            Sign in
          </Link>
        </Typography>
      </Stack>
    </AuthPageLayout>
  );
};

export default ResetPasswordPage;
