import * as React from "react";

import { Link, Stack, Typography } from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearchParams } from "react-router-dom";

import { useAuth } from "../auth";
import { AuthPageLayout, SignInForm } from "../components";

const SignInPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const auth = useAuth();

  const handleForgotPasswordClick = () => {
    const query = searchParams.toString();
    navigate(query ? `/forgot-password?${query}` : "/forgot-password");
  };

  const handleNavigateToSignUp = () => {
    const searchString = searchParams.toString();
    navigate(searchString ? `/sign-up?${searchString}` : "/sign-up");
  };

  const handleSignedIn = async (email: string) => {
    auth.login(email);
    await queryClient.invalidateQueries();
    const callbackUrl = searchParams.get("callbackUrl") ?? "/";
    navigate(callbackUrl, { replace: true });
  };

  return (
    <AuthPageLayout>
      <Typography variant="h5" align="center" gutterBottom>
        Sign in
      </Typography>

      <SignInForm onSignedIn={handleSignedIn} />

      <Stack spacing={2}>
        <Link
          component="button"
          type="button"
          onClick={handleForgotPasswordClick}
        >
          Forgot your password?
        </Link>

        <Typography
          variant="body2"
          color="text.secondary"
          align="center"
          component="div"
        >
          Don&apos;t have an account?{" "}
          <Link
            component="button"
            type="button"
            onClick={handleNavigateToSignUp}
          >
            Sign up
          </Link>
        </Typography>
      </Stack>
    </AuthPageLayout>
  );
};
export default SignInPage;
