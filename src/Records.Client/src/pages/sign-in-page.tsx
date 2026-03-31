import * as React from "react";

import { Link, Stack, Typography } from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";

import { useAuth } from "../auth";
import AuthPageLayout from "../components/auth-page-layout";
import SignInForm from "../components/sign-in-form";

const SignInPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const auth = useAuth();

  const handleForgotPasswordClick = () => {
    const query = searchParams.toString();
    navigate(query ? `/forgot-password?${query}` : "/forgot-password");
  };

  const handleNavigateToSignUp = () => {
    const searchString = searchParams.toString();
    navigate(searchString ? `/sign-up?${searchString}` : "/sign-up");
  };

  const handleSignedIn = async () => {
    const session = await auth.login();
    if (!session) {
      throw new Error("Authenticated session was not established.");
    }

    const callbackUrl = searchParams.get("callbackUrl") ?? "/";
    navigate(session.access.canAccessOps ? callbackUrl : "/access-denied", {
      replace: true,
    });
  };

  return (
    <AuthPageLayout>
      <Typography variant="h5" component="h1" align="center" gutterBottom>
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
