import * as React from "react";

import { Alert, Button, Stack, TextField } from "@mui/material";

interface ForgotPasswordFormProps {
  onSubmitStart?: () => void;
  onSuccess: (email: string) => void;
}

const ForgotPasswordForm = ({
  onSubmitStart,
  onSuccess,
}: ForgotPasswordFormProps) => {
  const [loading, setLoading] = React.useState(false);
  const [email, setEmail] = React.useState("");
  const [emailErrorMessage, setEmailErrorMessage] = React.useState<
    string | null
  >(null);
  const [formErrorMessage, setFormErrorMessage] = React.useState<string | null>(
    null,
  );
  const [submittedEmail, setSubmittedEmail] = React.useState<string>("");

  const validateInputs = () => {
    if (email && /\S+@\S+\.\S+/.test(email)) {
      setEmailErrorMessage(null);
      return true;
    }

    setEmailErrorMessage("Please enter a valid email address.");
    return false;
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmitStart?.();

    if (!validateInputs()) {
      return;
    }

    try {
      setFormErrorMessage(null);
      setLoading(true);
      setSubmittedEmail(email);

      const response = await fetch("/identity/forgotPassword", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email }),
      });

      if (!response.ok) {
        if (response.status === 400) {
          const errorData = await response.json();
          if (errorData.errors?.Email) {
            setEmailErrorMessage(errorData.errors.Email[0]);
          } else {
            setEmailErrorMessage("Please enter a valid email address.");
          }
        } else {
          setFormErrorMessage("Failed to send reset email. Please try again.");
        }
        return;
      }

      setEmail("");
      setEmailErrorMessage(null);
      onSuccess("Password reset link sent! Check your email.");
    } catch {
      setFormErrorMessage("An unexpected error occurred.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Stack
      spacing={3}
      component="form"
      method="post"
      onSubmit={handleSubmit}
      noValidate
    >
      <TextField
        id="email"
        name="email"
        placeholder="your@email.com"
        autoComplete="email"
        required
        fullWidth
        variant="outlined"
        type="email"
        value={email}
        onChange={(event) => setEmail(event.target.value)}
        error={emailErrorMessage !== null}
        helperText={emailErrorMessage}
      />

      <Button
        type="submit"
        variant="contained"
        size="large"
        fullWidth
        loading={loading}
        onClick={validateInputs}
      >
        Send reset link
      </Button>

      {formErrorMessage && (
        <Alert severity="error" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default ForgotPasswordForm;
