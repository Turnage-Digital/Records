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
  onSignedIn: (email: string) => Promise<void> | void;
}

const SignInForm = ({ onSignedIn }: Props) => {
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
    let retval = true;

    if (email && /\S+@\S+\.\S+/.test(email)) {
      setEmailErrorMessage(null);
    } else {
      setEmailErrorMessage("Please enter a valid email address.");
      retval = false;
    }

    if (password) {
      setPasswordErrorMessage(null);
    } else {
      setPasswordErrorMessage("Please enter a password.");
      retval = false;
    }

    return retval;
  };

  const emailErrorColor = emailErrorMessage ? "error" : "primary";
  const passwordErrorColor = passwordErrorMessage ? "error" : "primary";

  const showPasswordType = showPassword ? "text" : "password";
  const showPasswordIcon = showPassword ? <VisibilityOff /> : <Visibility />;

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
        autoComplete="current-password"
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
        <Alert severity="error" sx={{ width: "100%" }}>
          {formErrorMessage}
        </Alert>
      )}
    </Stack>
  );
};

export default SignInForm;
