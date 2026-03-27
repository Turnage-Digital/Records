import * as React from "react";

import {
  Autocomplete,
  Box,
  Checkbox,
  Stack,
  TextField,
  Typography,
} from "@mui/material";

import { Status, StatusTransition } from "../../models";

interface StatusTransitionsEditorProps {
  statuses: Status[];
  transitions: StatusTransition[];
  onChange: (next: StatusTransition[]) => void;
}

const StatusTransitionsEditor = ({
  statuses,
  transitions,
  onChange,
}: StatusTransitionsEditorProps) => {
  const statusNames = React.useMemo(
    () => statuses.map((status) => status.name),
    [statuses],
  );

  const transitionsMap = React.useMemo(() => {
    const map = new Map<string, string[]>();
    transitions.forEach((transition) => {
      map.set(transition.from, transition.allowedNext);
    });
    return map;
  }, [transitions]);

  const handleAllowedChange = React.useCallback(
    (from: string, allowed: string[]) => {
      const sanitized = allowed.filter((target) =>
        statusNames.includes(target),
      );
      const next = transitions.filter((transition) => transition.from !== from);
      if (sanitized.length > 0) {
        next.push({ from, allowedNext: sanitized });
      }
      onChange(next);
    },
    [onChange, statusNames, transitions],
  );

  if (statuses.length === 0) {
    return (
      <Typography color="text.secondary">
        Add at least one status before configuring transitions.
      </Typography>
    );
  }

  return (
    <Stack spacing={2}>
      {statuses.map((status) => {
        const options = statusNames.filter((name) => name !== status.name);
        const value = transitionsMap.get(status.name) ?? [];

        return (
          <Box key={status.name}>
            <Typography fontWeight={600} gutterBottom>
              {status.name}
            </Typography>
            <Autocomplete
              multiple
              disableCloseOnSelect
              options={options}
              value={value}
              onChange={(_, newValue) => {
                handleAllowedChange(status.name, newValue);
              }}
              renderInput={(params) => (
                <TextField
                  {...params}
                  placeholder="Select allowed next statuses"
                  InputProps={{
                    ...params.InputProps,
                    sx: {
                      backgroundColor: "background.paper",
                    },
                  }}
                />
              )}
              renderOption={(props, option, { selected }) => (
                <li {...props}>
                  <Checkbox sx={{ mr: 1 }} checked={selected} size="small" />
                  {option}
                </li>
              )}
            />
          </Box>
        );
      })}
    </Stack>
  );
};

export default StatusTransitionsEditor;
