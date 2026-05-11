import * as React from "react";

import { Stack, TextField } from "@mui/material";

interface EditRecordsetNameContentProps {
  name: string | null;
  onNameChanged: (name: string) => void;
  disabled?: boolean;
}

const EditRecordsetNameContent = ({
  name,
  onNameChanged,
  disabled,
}: EditRecordsetNameContentProps) => {
  return (
    <Stack spacing={2}>
      <TextField
        name="name"
        id="name"
        label="Name"
        margin="normal"
        required
        fullWidth
        sx={{
          background: "white",
        }}
        value={name ?? ""}
        onChange={(event) => {
          onNameChanged(event.target.value);
        }}
        disabled={disabled}
      />
    </Stack>
  );
};

export default EditRecordsetNameContent;
