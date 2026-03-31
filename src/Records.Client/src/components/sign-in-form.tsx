import * as React from "react";

import { Alert, Button, Stack, TextField } from "@mui/material";

import PasswordField from "./password-field";

interface SignInFormProps {
  onSignedIn: (email: string) => Promise<void> | void;
}

const SignInForm = ({ onSignedIn }: SignInFormProps) => {
  const [showPassword, setShowPassword] = React.useState(false);
  const [loading, setLoading] = React.useState(false);

  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");

  const [formErrorMessage, setFormErrorMessage] = React.useState<string | null>(
    null,
  );
  const [emailErrorMessage, setEmailErrorMessage] = React.useState<
    string | null
  >(null);
  const [passwordErrorMessage, setPasswordErrorMessage] = React.useState<
    string | null
  >(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (emailErrorMessage || passwordErrorMessage) {
      return;
    }

    try {
      setFormErrorMessage(null);
      setLoading(true);

      const input = { email, password };
      const request = new Request("/identity/login?useCookies=true", {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify(input),
      });
      const response = await fetch(request);
      if (!response.ok) {
        setFormErrorMessage("Invalid email or password.");
        return;
      }

      await onSignedIn(email);
    } catch {
      setFormErrorMessage("An unexpected error occurred.");
    } finally {
      setLoading(false);
    }
  };

  const validateInputs = () => {
    let isValid = true;

    if (email && /\S+@\S+\.\S+/.test(email)) {
      setEmailErrorMessage(null);
    } else {
      setEmailErrorMessage("Please enter a valid email address.");
      isValid = false;
    }

    if (password) {
      setPasswordErrorMessage(null);
    } else {
      setPasswordErrorMessage("Please enter a password.");
      isValid = false;
    }

    return isValid;
  };

  const emailErrorColor = emailErrorMessage ? "error" : "primary";

  return (
    <Stack
      spacing={3}
      component="form"
      method="post"
      onSubmit={handleSubmit}
      noValidate
    >
      <TextField
        margin="normal"
        id="email"
        name="email"
        label="Email"
        placeholder="your@email.com"
        autoComplete="email"
        required
        fullWidth
        variant="outlined"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        error={emailErrorMessage !== null}
        helperText={emailErrorMessage}
        color={emailErrorColor}
      />

      <PasswordField
        id="password"
        name="password"
        label="Password"
        autoComplete="current-password"
        value={password}
        onChange={setPassword}
        errorMessage={passwordErrorMessage}
        showAriaLabel="Show password"
        hideAriaLabel="Hide password"
      />

      <Button
        type="submit"
        variant="contained"
        size="large"
        fullWidth
        loading={loading}
        onClick={validateInputs}
      >
        Sign in
      </Button>

      {formErrorMessage && (
        <Alert severity="error" aria-live="assertive" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default SignInForm;
