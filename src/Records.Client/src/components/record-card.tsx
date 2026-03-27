import * as React from "react";

import {
  CalendarToday,
  CheckCircle,
  Delete,
  Edit,
  Numbers,
  Receipt,
  TextFields,
  Visibility,
} from "@mui/icons-material";
import {
  alpha,
  Box,
  Card,
  CardActions,
  Divider,
  Grid,
  IconButton,
  Stack,
  Tooltip,
  Typography,
  useTheme,
} from "@mui/material";

import {
  ColumnType,
  getStatusFromName,
  RecordItem,
  RecordsetItemDefinition,
} from "../models";
import StatusChip from "./status-chip";

interface RecordCardProps {
  record: RecordItem;
  definition: RecordsetItemDefinition;
  onViewRecord?: (recordsetId: string, recordId: number) => void;
  onEditRecord?: (recordsetId: string, recordId: number) => void;
  onDeleteRecord?: (recordsetId: string, recordId: number) => void;
}

const RecordCard = ({
  record,
  definition,
  onViewRecord,
  onEditRecord,
  onDeleteRecord,
}: RecordCardProps) => {
  const theme = useTheme();
  const rawStatus = record.bag.status;
  const status = getStatusFromName(definition.statuses, rawStatus);
  const statusChip = status && <StatusChip status={status} />;

  const viewAction = onViewRecord ? (
    <Tooltip title={`View record ${record.id}`}>
      <IconButton
        onClick={() => onViewRecord(definition.id!, record.id!)}
        color="primary"
        aria-label={`View record ${record.id}`}
      >
        <Visibility aria-hidden="true" />
      </IconButton>
    </Tooltip>
  ) : null;

  const editAction = onEditRecord ? (
    <Tooltip title={`Edit record ${record.id}`}>
      <IconButton
        onClick={() => onEditRecord(definition.id!, record.id!)}
        color="primary"
        aria-label={`Edit record ${record.id}`}
      >
        <Edit aria-hidden="true" />
      </IconButton>
    </Tooltip>
  ) : null;

  const deleteAction = onDeleteRecord ? (
    <Tooltip title={`Delete record ${record.id}`}>
      <IconButton
        onClick={() => onDeleteRecord(definition.id!, record.id!)}
        color="error"
        aria-label={`Delete record ${record.id}`}
      >
        <Delete aria-hidden="true" />
      </IconButton>
    </Tooltip>
  ) : null;

  const getFieldIcon = (type: ColumnType) => {
    switch (type) {
      case ColumnType.Date:
        return <CalendarToday sx={{ fontSize: 16, color: "text.secondary" }} />;
      case ColumnType.Boolean:
        return <CheckCircle sx={{ fontSize: 16, color: "text.secondary" }} />;
      case ColumnType.Number:
        return <Numbers sx={{ fontSize: 16, color: "text.secondary" }} />;
      case ColumnType.Text:
      default:
        return <TextFields sx={{ fontSize: 16, color: "text.secondary" }} />;
    }
  };

  const getDisplayValue = (
    rawValue: any,
    type: ColumnType,
  ): React.ReactNode => {
    if (rawValue === null || rawValue === undefined || rawValue === "") {
      return "-";
    }

    switch (type) {
      case ColumnType.Date:
        return new Date(rawValue).toLocaleDateString();
      case ColumnType.Boolean:
        return rawValue ? "True" : "False";
      case ColumnType.Number:
      case ColumnType.Text:
      default:
        return String(rawValue);
    }
  };

  const statusChipNode = statusChip ? (
    <Box sx={{ ml: "auto" }}>{statusChip}</Box>
  ) : null;

  return (
    <Card
      variant="outlined"
      sx={{
        display: "flex",
        flexDirection: "column",
        gap: 3,
        p: 3,
        maxWidth: 500,
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
          <Receipt sx={{ fontSize: 22 }} />
        </Box>
        {statusChipNode}
      </Stack>

      <Typography variant="h6" component="div">
        ID {record.id}
      </Typography>

      <Box sx={{ flexGrow: 1 }}>
        <Grid container>
          {definition.columns.map((column, index) => {
            const key = column.property ?? column.name;
            const rawValue = record.bag[key];
            const displayValue = getDisplayValue(rawValue, column.type);
            const isLastItem = index === definition.columns.length - 1;
            const fontWeight =
              column.type === ColumnType.Number ? "medium" : "normal";

            return (
              <React.Fragment key={key}>
                <Grid size={{ xs: 5 }}>
                  <Box
                    sx={{
                      display: "flex",
                      alignItems: "center",
                      gap: 1,
                      height: "100%",
                      minHeight: 40,
                    }}
                  >
                    {getFieldIcon(column.type)}
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      fontWeight="medium"
                      sx={{ textTransform: "uppercase", letterSpacing: 0.5 }}
                    >
                      {column.name}
                    </Typography>
                  </Box>
                </Grid>
                <Grid size={{ xs: 7 }}>
                  <Box
                    sx={{
                      display: "flex",
                      alignItems: "center",
                      height: "100%",
                      minHeight: 40,
                      pl: 2,
                    }}
                  >
                    <Typography
                      variant="body1"
                      color="text.primary"
                      fontWeight={fontWeight}
                      sx={{
                        fontSize: "1rem",
                        lineHeight: 1.5,
                      }}
                    >
                      {displayValue}
                    </Typography>
                  </Box>
                </Grid>
                {!isLastItem && (
                  <Grid size={{ xs: 12 }} sx={{ my: 1.5 }}>
                    <Divider
                      sx={{
                        mx: -3,
                        borderColor: theme.palette.divider,
                      }}
                    />
                  </Grid>
                )}
              </React.Fragment>
            );
          })}
        </Grid>
      </Box>

      <CardActions sx={{ justifyContent: "flex-end", gap: 1, pt: 1 }}>
        {viewAction}
        {editAction}
        {deleteAction}
      </CardActions>
    </Card>
  );
};

export default RecordCard;
