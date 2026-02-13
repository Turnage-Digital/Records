import * as React from "react";

import { Delete, Dataset, Edit, Visibility } from "@mui/icons-material";
import {
  alpha,
  Box,
  Card,
  CardActions,
  Chip,
  IconButton,
  Stack,
  Tooltip,
  Typography,
  useTheme,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import { RecordsetName } from "../models";

interface Props {
  recordsetName: RecordsetName;
  onDeleteClick: (recordset: RecordsetName) => void;
}

const RecordsetCard = ({ recordsetName, onDeleteClick }: Props) => {
  const theme = useTheme();

  return (
    <Card
      variant="outlined"
      sx={{
        display: "flex",
        flexDirection: "column",
        minHeight: 220,
        p: 3,
        gap: 3,
        borderColor: theme.palette.divider,
        boxShadow: "none",
        transition: "border-color 0.2s ease, box-shadow 0.2s ease",
        "&:hover": {
          borderColor: alpha(theme.palette.primary.main, 0.24),
          boxShadow: theme.shadows[2],
        },
      }}
    >
      <Stack
        direction="row"
        alignItems="center"
        justifyContent="space-between"
        spacing={2}
      >
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            width: 44,
            height: 44,
            borderRadius: (theme) => theme.shape.borderRadius,
            backgroundColor: theme.palette.grey[100],
            color: theme.palette.primary.dark,
          }}
        >
          <Dataset sx={{ fontSize: 22 }} />
        </Box>
        <Chip
          label={`${recordsetName.count} records`}
          size="small"
          color="primary"
          variant="outlined"
        />
      </Stack>

      <Typography variant="h6" component="div">
        {recordsetName.name}
      </Typography>

      <Box sx={{ flexGrow: 1 }} />

      <CardActions sx={{ justifyContent: "flex-end", gap: 1 }}>
        <Tooltip title={`View ${recordsetName.name}`}>
          <IconButton
            component={RouterLink}
            to={`/${recordsetName.id}?page=0&pageSize=10`}
            color="primary"
          >
            <Visibility />
          </IconButton>
        </Tooltip>
        <Tooltip title={`Edit ${recordsetName.name}`}>
          <IconButton
            component={RouterLink}
            to={`/${recordsetName.id}/edit`}
            color="primary"
          >
            <Edit />
          </IconButton>
        </Tooltip>
        <Tooltip title={`Delete ${recordsetName.name}`}>
          <IconButton
            onClick={() => onDeleteClick(recordsetName)}
            color="error"
          >
            <Delete />
          </IconButton>
        </Tooltip>
      </CardActions>
    </Card>
  );
};

export default RecordsetCard;
