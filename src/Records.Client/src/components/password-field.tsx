import * as React from "react";

import { Visibility, VisibilityOff } from "@mui/icons-material";
import { IconButton, InputAdornment, TextField } from "@mui/material";

interface PasswordFieldProps {
  autoComplete: string;
  errorMessage?: string | null;
  hideAriaLabel: string;
  id: string;
  label: string;
  name: string;
  onChange: (value: string) => void;
  placeholder?: string;
  showAriaLabel: string;
  value: string;
}

const PasswordField = ({
  autoComplete,
  errorMessage = null,
  hideAriaLabel,
  id,
  label,
  name,
  onChange,
  placeholder = "••••••",
  showAriaLabel,
  value,
}: PasswordFieldProps) => {
  const [isPasswordVisible, setIsPasswordVisible] = React.useState(false);

  const inputType = isPasswordVisible ? "text" : "password";
  const visibilityIcon = isPasswordVisible ? <VisibilityOff /> : <Visibility />;
  const visibilityAriaLabel = isPasswordVisible ? hideAriaLabel : showAriaLabel;
  const color = errorMessage ? "error" : "primary";

  return (
    <TextField
      margin="normal"
      id={id}
      name={name}
      label={label}
      placeholder={placeholder}
      autoComplete={autoComplete}
      required
      fullWidth
      variant="outlined"
      type={inputType}
      value={value}
      onChange={(event) => onChange(event.target.value)}
      error={errorMessage !== null}
      helperText={errorMessage}
      color={color}
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                aria-label={visibilityAriaLabel}
                aria-pressed={isPasswordVisible}
                onClick={() => setIsPasswordVisible((previous) => !previous)}
              >
                {visibilityIcon}
              </IconButton>
            </InputAdornment>
          ),
        },
      }}
    />
  );
};

export default PasswordField;
