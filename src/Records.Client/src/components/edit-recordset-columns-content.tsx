import * as React from "react";
import { useState } from "react";

import { Delete } from "@mui/icons-material";
import {
  Box,
  Button,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
} from "@mui/material";

import { ColumnType } from "../models/column-type";

import type { Column } from "../models/column";

interface EditRecordsetColumnsContentProps {
  columns: Column[] | null;
  onColumnsChanged: (columns: Column[]) => void;
}

const EditRecordsetColumnsContent = ({
  columns,
  onColumnsChanged,
}: EditRecordsetColumnsContentProps) => {
  const [columnName, setColumnName] = useState<string | null>(null);
  const [columnType, setColumnType] = useState<ColumnType | null>(null);

  const handleAddClicked = () => {
    const existingColumns = columns ?? [];
    const nextColumns = [
      ...existingColumns,
      { name: columnName!, type: columnType! },
    ];

    setColumnName(null);
    setColumnType(null);
    onColumnsChanged(nextColumns);
  };

  const handleRemoveClicked = (name: string) => {
    const existingColumns = columns ?? [];
    const updatedColumns = existingColumns.filter(
      (column) => column.name !== name,
    );

    onColumnsChanged(updatedColumns);
  };

  const columnTypes = Object.values(ColumnType);

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2}>
        <TextField
          name="name"
          id="name"
          label="Name"
          margin="normal"
          fullWidth
          sx={{
            background: "white",
          }}
          value={columnName ?? ""}
          onChange={(event) => setColumnName(event.target.value)}
        />

        <FormControl variant="outlined" margin="normal" fullWidth>
          <InputLabel htmlFor="type">Type</InputLabel>
          <Select
            variant="outlined"
            name="type"
            id="type"
            label="Type"
            sx={{
              background: "white",
            }}
            value={columnType ?? ""}
            onChange={(event) =>
              setColumnType(event.target.value as ColumnType)
            }
          >
            {columnTypes.map((columnType) => (
              <MenuItem key={columnType} value={columnType}>
                {columnType}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        <Box sx={{ display: "flex", alignItems: "center" }}>
          <Button
            variant="contained"
            color="primary"
            onClick={handleAddClicked}
            disabled={!columnName || !columnType}
          >
            Add
          </Button>
        </Box>
      </Stack>

      {columns && columns.length > 0 && (
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {columns.map((column) => (
                <TableRow key={column.name}>
                  <TableCell>{column.name}</TableCell>
                  <TableCell>{column.type}</TableCell>
                  <TableCell>
                    <IconButton
                      onClick={() => handleRemoveClicked(column.name)}
                      aria-label={`Remove column ${column.name}`}
                    >
                      <Delete />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Stack>
  );
};

export default EditRecordsetColumnsContent;
