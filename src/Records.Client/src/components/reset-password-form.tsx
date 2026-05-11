import * as React from "react";

import { Alert, Button, Stack } from "@mui/material";

import PasswordField from "./password-field";

interface ResetPasswordFormProps {
  email: string;
  resetCode: string;
  onPasswordReset: () => Promise<void> | void;
}

const ResetPasswordForm = ({
  email,
  resetCode,
  onPasswordReset,
}: ResetPasswordFormProps) => {
  const [loading, setLoading] = React.useState(false);

  const [password, setPassword] = React.useState("");
  const [confirmPassword, setConfirmPassword] = React.useState("");

  const [formErrorMessage, setFormErrorMessage] = React.useState<string | null>(
    null,
  );
  const [passwordErrorMessage, setPasswordErrorMessage] = React.useState<
    string | null
  >(null);
  const [confirmPasswordErrorMessage, setConfirmPasswordErrorMessage] =
    React.useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (passwordErrorMessage || confirmPasswordErrorMessage) {
      return;
    }

    if (password !== confirmPassword) {
      setConfirmPasswordErrorMessage("Passwords do not match.");
      return;
    }

    try {
      setFormErrorMessage(null);
      setLoading(true);

      const input = {
        email,
        resetCode,
        newPassword: password,
      };
      const request = new Request("/identity/resetPassword", {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify(input),
      });
      const response = await fetch(request);

      if (!response.ok) {
        const errorData = await response.json();

        if (response.status === 400 && errorData.errors) {
          const errors = errorData.errors;
          if (errors.NewPassword) {
            setPasswordErrorMessage(errors.NewPassword[0]);
          } else if (errors.ResetCode) {
            setFormErrorMessage("Invalid or expired reset code.");
          } else {
            setFormErrorMessage("Invalid reset request.");
          }
        } else {
          setFormErrorMessage("Failed to reset password. Please try again.");
        }
        return;
      }

      await onPasswordReset();
    } catch {
      setFormErrorMessage("An unexpected error occurred.");
    } finally {
      setLoading(false);
    }
  };

  const validateInputs = () => {
    let isValid = true;

    if (password) {
      setPasswordErrorMessage(null);
    } else {
      setPasswordErrorMessage("Please enter a password.");
      isValid = false;
    }

    if (confirmPassword) {
      if (password === confirmPassword) {
        setConfirmPasswordErrorMessage(null);
      } else {
        setConfirmPasswordErrorMessage("Passwords do not match.");
        isValid = false;
      }
    } else {
      setConfirmPasswordErrorMessage("Please confirm your password.");
      isValid = false;
    }

    return isValid;
  };

  return (
    <Stack
      spacing={3}
      component="form"
      method="post"
      onSubmit={handleSubmit}
      noValidate
    >
      <PasswordField
        id="password"
        name="password"
        label="New password"
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
        label="Confirm new password"
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
        Reset Password
      </Button>

      {formErrorMessage && (
        <Alert severity="error" aria-live="assertive" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default ResetPasswordForm;
