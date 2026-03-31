import * as React from "react";

import { Alert, Button, Stack, TextField } from "@mui/material";

import PasswordField from "./password-field";

interface SignUpFormProps {
  onSignedUp: (email: string) => Promise<void> | void;
}

const SignUpForm = ({ onSignedUp }: SignUpFormProps) => {
  const [loading, setLoading] = React.useState(false);

  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [confirmPassword, setConfirmPassword] = React.useState("");

  const [formErrorMessage, setFormErrorMessage] = React.useState<string | null>(
    null,
  );
  const [emailErrorMessage, setEmailErrorMessage] = React.useState<
    string | null
  >(null);
  const [passwordErrorMessage, setPasswordErrorMessage] = React.useState<
    string | null
  >(null);
  const [confirmPasswordErrorMessage, setConfirmPasswordErrorMessage] =
    React.useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (
      emailErrorMessage ||
      passwordErrorMessage ||
      confirmPasswordErrorMessage
    ) {
      return;
    }

    try {
      setFormErrorMessage(null);
      setLoading(true);

      const input = { email, password };
      const request = new Request("/identity/register", {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify(input),
      });
      const response = await fetch(request);
      if (!response.ok) {
        const errorData = await response.json();
        if (errorData.errors && Object.keys(errorData.errors).length > 0) {
          const firstError = Object.values(errorData.errors)[0] as string[];
          setFormErrorMessage(firstError[0]);
        } else {
          setFormErrorMessage("Registration failed. Please try again.");
        }
        return;
      }

      await onSignedUp(email);
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

    if (password && password.length >= 6) {
      setPasswordErrorMessage(null);
    } else {
      setPasswordErrorMessage("Password must be at least 6 characters long.");
      isValid = false;
    }

    if (confirmPassword && confirmPassword === password) {
      setConfirmPasswordErrorMessage(null);
    } else {
      setConfirmPasswordErrorMessage("Passwords do not match.");
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
        autoComplete="new-password"
        value={password}
        onChange={setPassword}
        errorMessage={passwordErrorMessage}
        showAriaLabel="Show password"
        hideAriaLabel="Hide password"
      />

      <PasswordField
        id="confirmPassword"
        name="confirmPassword"
        label="Confirm password"
        autoComplete="new-password"
        value={confirmPassword}
        onChange={setConfirmPassword}
        errorMessage={confirmPasswordErrorMessage}
        showAriaLabel="Show confirmation password"
        hideAriaLabel="Hide confirmation password"
      />

      <Button
        type="submit"
        variant="contained"
        size="large"
        fullWidth
        loading={loading}
        onClick={validateInputs}
      >
        Sign up
      </Button>

      {formErrorMessage && (
        <Alert severity="error" aria-live="assertive" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default SignUpForm;
