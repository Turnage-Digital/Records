import * as React from "react";

import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Box,
  Button,
  Chip,
  Divider,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

import { recordDetailsPath } from "../../lib/routes";

import type {
  WorkspaceArtifact,
  WorkspaceAttribute,
  WorkspaceEditorSchema,
  WorkspaceEntity,
  WorkspaceGrid,
  WorkspaceHistory,
  WorkspaceNotification,
  WorkspaceNotifications,
  WorkspaceProposal,
  WorkspaceSchemaField,
} from "../../models/agent";

interface AgentWorkSurfaceProps {
  artifact?: WorkspaceArtifact | null;
  proposal?: WorkspaceProposal | null;
  isBusy: boolean;
  onConfirm: (proposalId: string) => void;
  onReject: (proposalId: string) => void;
}

function renderValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return (
      <Typography component="span" color="text.secondary">
        Empty
      </Typography>
    );
  }

  if (typeof value === "object") {
    return JSON.stringify(value);
  }

  return String(value);
}

function attributeValue(attribute?: WorkspaceAttribute) {
  return attribute?.displayValue ?? attribute?.value;
}

function renderGridSection(grid: WorkspaceGrid) {
  const resultLabel = `${grid.totalCount} result${grid.totalCount === 1 ? "" : "s"}`;
  const filtersNode =
    grid.resolvedFilters.length === 0 ? null : (
      <Stack direction="row" flexWrap="wrap" gap={1}>
        {grid.resolvedFilters.map((filter) => (
          <Chip key={filter} label={filter} size="small" variant="outlined" />
        ))}
      </Stack>
    );

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          spacing={2}
        >
          <Typography variant="h6">{grid.collectionLabel} results</Typography>
          <Chip color="primary" variant="outlined" label={resultLabel} />
        </Stack>

        {filtersNode}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                {grid.columns.map((column) => (
                  <TableCell key={column.key}>{column.label}</TableCell>
                ))}
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {grid.rows.map((row) => (
                <TableRow key={row.entityId} hover>
                  {grid.columns.map((column) => {
                    const attribute = row.attributes.find(
                      (item) => item.key === column.key,
                    );

                    return (
                      <TableCell key={`${row.entityId}-${column.key}`}>
                        {renderValue(attributeValue(attribute))}
                      </TableCell>
                    );
                  })}
                  <TableCell align="right">
                    <Button
                      component={RouterLink}
                      to={recordDetailsPath(grid.collectionId, row.entityId)}
                      size="small"
                      endIcon={<OpenInNewIcon fontSize="small" />}
                    >
                      Open
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Stack>
    </Paper>
  );
}

function renderDetailSection(detail: WorkspaceEntity) {
  const openFullPageButton = detail.entityId ? (
    <Button
      component={RouterLink}
      to={recordDetailsPath(detail.collectionId, detail.entityId)}
      endIcon={<OpenInNewIcon fontSize="small" />}
    >
      Open full page
    </Button>
  ) : null;

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          spacing={2}
        >
          <Typography variant="h6">{detail.displayName}</Typography>
          {openFullPageButton}
        </Stack>

        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(auto-fit, minmax(180px, 1fr))",
            },
          }}
        >
          {detail.attributes.map((attribute) => (
            <Paper
              key={attribute.key}
              variant="outlined"
              sx={{ px: 2, py: 1.5, backgroundColor: "grey.50" }}
            >
              <Typography variant="caption" color="text.secondary">
                {attribute.label}
              </Typography>
              <Typography variant="body2" sx={{ mt: 0.5 }}>
                {renderValue(attributeValue(attribute))}
              </Typography>
            </Paper>
          ))}
        </Box>
      </Stack>
    </Paper>
  );
}

function renderAllowedValues(field: WorkspaceSchemaField) {
  if (field.allowedValues.length === 0) {
    return null;
  }

  return (
    <Stack direction="row" flexWrap="wrap" gap={1}>
      {field.allowedValues.map((value) => (
        <Chip key={`${field.key}-${value}`} label={value} size="small" />
      ))}
    </Stack>
  );
}

function renderFieldCard(field: WorkspaceSchemaField) {
  const requiredLabel = field.required ? "Required" : "Optional";
  const validationHintChip = field.validationHint ? (
    <Chip label={field.validationHint} size="small" variant="outlined" />
  ) : null;
  const allowedValues = renderAllowedValues(field);

  return (
    <Paper
      key={field.key}
      variant="outlined"
      sx={{ px: 2, py: 1.75, backgroundColor: "grey.50" }}
    >
      <Stack spacing={1.25}>
        <Stack direction="row" justifyContent="space-between" spacing={1.5}>
          <Typography variant="subtitle2">{field.label}</Typography>
          <Typography variant="caption" color="text.secondary">
            {field.type}
          </Typography>
        </Stack>

        <Stack direction="row" flexWrap="wrap" gap={1}>
          <Chip label={requiredLabel} size="small" variant="outlined" />
          {validationHintChip}
        </Stack>

        {allowedValues}
      </Stack>
    </Paper>
  );
}

function renderEditorSection(editor: WorkspaceEditorSchema) {
  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Typography variant="h6">Editor schema</Typography>
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(auto-fit, minmax(220px, 1fr))",
            },
          }}
        >
          {editor.fields.map(renderFieldCard)}
        </Box>
      </Stack>
    </Paper>
  );
}

function renderHistorySection(history: WorkspaceHistory) {
  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Typography variant="h6">Activity</Typography>
        <Stack divider={<Divider flexItem />} spacing={0}>
          {history.entries.map((entry) => {
            const entryKey = `${entry.type}-${entry.occurredAt}-${entry.actorId ?? "unknown"}`;
            const occurredAt = new Date(entry.occurredAt).toLocaleString();
            const actorLabel = entry.actorId ?? "Unknown actor";

            return (
              <Box key={entryKey} sx={{ py: 1.5 }}>
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  justifyContent="space-between"
                  spacing={1}
                >
                  <Typography variant="subtitle2">{entry.type}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {occurredAt}
                  </Typography>
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  {actorLabel}
                </Typography>
              </Box>
            );
          })}
        </Stack>
      </Stack>
    </Paper>
  );
}

function renderNotificationItem(item: WorkspaceNotification) {
  const occurredAt = new Date(item.occurredAt).toLocaleString();

  return (
    <Box key={item.id} sx={{ py: 1.5 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        justifyContent="space-between"
        spacing={1}
      >
        <Typography variant="subtitle2">{item.title}</Typography>
        <Typography variant="body2" color="text.secondary">
          {occurredAt}
        </Typography>
      </Stack>
      <Typography variant="body2" color="text.secondary">
        {item.body}
      </Typography>
    </Box>
  );
}

function renderNotificationsSection(notifications: WorkspaceNotifications) {
  const unreadLabel = `${notifications.unreadCount} unread`;

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          spacing={2}
        >
          <Typography variant="h6">Notifications</Typography>
          <Chip label={unreadLabel} color="primary" variant="outlined" />
        </Stack>

        <Stack divider={<Divider flexItem />} spacing={0}>
          {notifications.items.map(renderNotificationItem)}
        </Stack>
      </Stack>
    </Paper>
  );
}

function renderProposalSection(
  proposal: WorkspaceProposal,
  isBusy: boolean,
  onConfirm: (proposalId: string) => void,
  onReject: (proposalId: string) => void,
) {
  const proposalLabel =
    proposal.kind === "create" ? "Create draft" : "Update proposal";
  const openRecordButton =
    proposal.kind === "create" || !proposal.target.entityId ? null : (
      <Button
        component={RouterLink}
        to={recordDetailsPath(
          proposal.target.collectionId,
          proposal.target.entityId,
        )}
        endIcon={<OpenInNewIcon fontSize="small" />}
      >
        Open record
      </Button>
    );
  const sourceExcerptNode = proposal.sourceExcerpt ? (
    <Paper
      variant="outlined"
      sx={{ px: 2, py: 1.5, backgroundColor: "background.paper" }}
    >
      <Typography
        component="pre"
        variant="body2"
        sx={{
          m: 0,
          whiteSpace: "pre-wrap",
          fontFamily: "monospace",
        }}
      >
        {proposal.sourceExcerpt}
      </Typography>
    </Paper>
  ) : null;

  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2.5,
        borderColor: "primary.main",
        background:
          "linear-gradient(180deg, rgba(25,118,210,0.06) 0%, rgba(25,118,210,0.02) 100%)",
      }}
    >
      <Stack spacing={2}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          spacing={2}
        >
          <Box>
            <Stack direction="row" spacing={1} alignItems="center">
              <Typography variant="h6">
                {proposal.target.displayName}
              </Typography>
              <Chip size="small" color="primary" label={proposalLabel} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {proposal.rationale}
            </Typography>
          </Box>

          <Stack direction="row" spacing={1}>
            {openRecordButton}
            <Button
              variant="outlined"
              disabled={isBusy}
              onClick={() => onReject(proposal.proposalId)}
            >
              Reject
            </Button>
            <Button
              variant="contained"
              disabled={isBusy}
              onClick={() => onConfirm(proposal.proposalId)}
            >
              Confirm
            </Button>
          </Stack>
        </Stack>

        {sourceExcerptNode}

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Field</TableCell>
                <TableCell>Before</TableCell>
                <TableCell>After</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {proposal.diffs.map((diff) => (
                <TableRow key={diff.key}>
                  <TableCell>{diff.label}</TableCell>
                  <TableCell>{renderValue(diff.before)}</TableCell>
                  <TableCell>{renderValue(diff.after)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Stack>
    </Paper>
  );
}

const AgentWorkSurface = ({
  artifact,
  proposal,
  isBusy,
  onConfirm,
  onReject,
}: AgentWorkSurfaceProps) => {
  const grid = artifact?.grid ?? null;
  const detail = artifact?.detail ?? null;
  const editor = artifact?.editor ?? null;
  const history = artifact?.history ?? null;
  const notifications = artifact?.notifications ?? null;
  const surfaceTitle =
    artifact?.title ?? proposal?.target.displayName ?? "Structured output";

  const gridSection = grid ? renderGridSection(grid) : null;
  const detailSection = detail ? renderDetailSection(detail) : null;
  const editorSection = editor ? renderEditorSection(editor) : null;
  const historySection = history ? renderHistorySection(history) : null;
  const notificationsSection = notifications
    ? renderNotificationsSection(notifications)
    : null;
  const proposalSection = proposal
    ? renderProposalSection(proposal, isBusy, onConfirm, onReject)
    : null;
  const emptyState =
    artifact || proposal ? null : (
      <Paper
        variant="outlined"
        sx={{
          px: 3,
          py: 4,
          textAlign: "center",
          backgroundColor: "grey.50",
        }}
      >
        <Typography variant="body1">
          Start with a natural-language request and the structured work surface
          will appear when the task needs it.
        </Typography>
      </Paper>
    );

  return (
    <Paper
      variant="outlined"
      sx={{
        display: "flex",
        flexDirection: "column",
        minHeight: { xs: "auto", md: "calc(100vh - 220px)" },
      }}
    >
      <Box sx={{ px: 3, py: 2.5, borderBottom: 1, borderColor: "divider" }}>
        <Typography
          variant="overline"
          sx={{ letterSpacing: "0.14em", color: "text.secondary" }}
        >
          Work Surface
        </Typography>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {surfaceTitle}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Search results, detail, history, and write proposals show up here when
          the task needs more than plain chat.
        </Typography>
      </Box>

      <Stack spacing={2.5} sx={{ flex: 1, overflow: "auto", px: 3, py: 3 }}>
        {gridSection}
        {detailSection}
        {editorSection}
        {historySection}
        {notificationsSection}
        {proposalSection}
        {emptyState}
      </Stack>
    </Paper>
  );
};

export default AgentWorkSurface;
