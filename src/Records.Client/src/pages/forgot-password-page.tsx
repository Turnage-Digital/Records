import * as React from "react";

import { Button, Link, Stack, Typography } from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";

import { AuthPageLayout, ForgotPasswordForm } from "../components";

const ForgotPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [successEmail, setSuccessEmail] = React.useState<string | null>(null);

  const handleNavigateToSignIn = () => {
    const query = searchParams.toString();
    navigate(query ? `/sign-in?${query}` : "/sign-in");
  };

  const handleSubmitStart = () => {
    setSuccessEmail(null);
  };

  const handleSubmitSuccess = (email: string) => {
    setSuccessEmail(email);
  };

  const content = successEmail ? (
    <Stack spacing={2} alignItems="center">
      <Typography variant="h5" align="center" gutterBottom>
        Check your inbox
      </Typography>

      <Typography variant="body2" color="text.secondary" align="center">
        We sent a password reset link to the address on file. Follow the
        instructions in that email to finish resetting your password.
      </Typography>

      <Button
        variant="contained"
        size="large"
        fullWidth
        onClick={handleNavigateToSignIn}
      >
        Return to sign in
      </Button>
    </Stack>
  ) : (
    <>
      <Stack spacing={2} alignItems="center">
        <Typography variant="h5" align="center" gutterBottom>
          Forgot password
        </Typography>
        <Typography variant="body2" color="text.secondary" align="center">
          Enter your account&apos;s email address and we&apos;ll send you a
          reset link.
        </Typography>
      </Stack>

      <ForgotPasswordForm
        onSubmitStart={handleSubmitStart}
        onSuccess={handleSubmitSuccess}
      />

      <Typography
        variant="body2"
        color="text.secondary"
        align="center"
        component="div"
      >
        Remembered your password?{" "}
        <Link component="button" type="button" onClick={handleNavigateToSignIn}>
          Sign in
        </Link>
      </Typography>
    </>
  );

  return <AuthPageLayout>{content}</AuthPageLayout>;
};

export default ForgotPasswordPage;
