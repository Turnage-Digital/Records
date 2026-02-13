import * as React from "react";

import { Visibility, VisibilityOff } from "@mui/icons-material";
import {
  Alert,
  Button,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
} from "@mui/material";

interface Props {
  onSignedUp: (email: string) => Promise<void> | void;
}

const SignUpForm = ({ onSignedUp }: Props) => {
  const [showPassword, setShowPassword] = React.useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false);
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
    let retval = true;

    if (email && /\S+@\S+\.\S+/.test(email)) {
      setEmailErrorMessage(null);
    } else {
      setEmailErrorMessage("Please enter a valid email address.");
      retval = false;
    }

    if (password && password.length >= 6) {
      setPasswordErrorMessage(null);
    } else {
      setPasswordErrorMessage("Password must be at least 6 characters long.");
      retval = false;
    }

    if (confirmPassword && confirmPassword === password) {
      setConfirmPasswordErrorMessage(null);
    } else {
      setConfirmPasswordErrorMessage("Passwords do not match.");
      retval = false;
    }

    return retval;
  };

  const emailErrorColor = emailErrorMessage ? "error" : "primary";
  const passwordErrorColor = passwordErrorMessage ? "error" : "primary";
  const confirmPasswordErrorColor = confirmPasswordErrorMessage
    ? "error"
    : "primary";

  const showPasswordType = showPassword ? "text" : "password";
  const showPasswordIcon = showPassword ? <VisibilityOff /> : <Visibility />;
  const showConfirmPasswordType = showConfirmPassword ? "text" : "password";
  const showConfirmPasswordIcon = showConfirmPassword ? (
    <VisibilityOff />
  ) : (
    <Visibility />
  );

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

      <TextField
        margin="normal"
        id="password"
        name="password"
        placeholder="••••••"
        autoComplete="new-password"
        required
        fullWidth
        variant="outlined"
        type={showPasswordType}
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        error={passwordErrorMessage !== null}
        helperText={passwordErrorMessage}
        color={passwordErrorColor}
        slotProps={{
          input: {
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  aria-label="toggle password visibility"
                  onClick={() => setShowPassword((prev) => !prev)}
                >
                  {showPasswordIcon}
                </IconButton>
              </InputAdornment>
            ),
          },
        }}
      />

      <TextField
        margin="normal"
        id="confirmPassword"
        name="confirmPassword"
        placeholder="••••••"
        autoComplete="new-password"
        required
        fullWidth
        variant="outlined"
        type={showConfirmPasswordType}
        value={confirmPassword}
        onChange={(e) => setConfirmPassword(e.target.value)}
        error={confirmPasswordErrorMessage !== null}
        helperText={confirmPasswordErrorMessage}
        color={confirmPasswordErrorColor}
        slotProps={{
          input: {
            endAdornment: (
              <InputAdornment position="end">
                <IconButton
                  aria-label="toggle confirm password visibility"
                  onClick={() => setShowConfirmPassword((prev) => !prev)}
                >
                  {showConfirmPasswordIcon}
                </IconButton>
              </InputAdornment>
            ),
          },
        }}
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
        <Alert severity="error" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default SignUpForm;
