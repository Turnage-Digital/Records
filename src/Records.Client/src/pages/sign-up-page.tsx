import * as React from "react";

import { Link, Stack, Typography } from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";

import { useAuth } from "../auth";
import AuthPageLayout from "../components/auth-page-layout";
import SignUpForm from "../components/sign-up-form";

const SignUpPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const auth = useAuth();

  const handleNavigateToSignIn = () => {
    const searchString = searchParams.toString();
    navigate(searchString ? `/sign-in?${searchString}` : "/sign-in");
  };

  const handleSignedUp = async () => {
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
        Sign up
      </Typography>

      <SignUpForm onSignedUp={handleSignedUp} />

      <Stack spacing={2}>
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
export default SignUpPage;
