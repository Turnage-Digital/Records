import * as React from "react";

import {
  Alert,
  Box,
  Button,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";

import type { AgentTurn } from "../../models/agent";

interface AgentConversationPaneProps {
  turns: AgentTurn[];
  message: string;
  pastedText: string;
  isBusy: boolean;
  onMessageChange: (value: string) => void;
  onPastedTextChange: (value: string) => void;
  onSubmit: () => void;
}

const AgentConversationPane = ({
  turns,
  message,
  pastedText,
  isBusy,
  onMessageChange,
  onPastedTextChange,
  onSubmit,
}: AgentConversationPaneProps) => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit();
  };

  const isSendDisabled =
    isBusy || (message.trim().length === 0 && pastedText.trim().length === 0);
  const sendButtonLabel = isBusy ? "Working..." : "Send";
  const emptyState =
    turns.length === 0 ? (
      <Alert severity="info" variant="outlined">
        Start with something like “show me all orders for Acme yesterday” or
        “create an order for Acme with status Open.”
      </Alert>
    ) : null;
  const turnCards = turns.map((turn) => {
    const isAssistant = turn.role === "assistant";
    const isSystem = turn.role === "system";
    const alignSelf = isAssistant || isSystem ? "flex-start" : "flex-end";
    const createdTime = new Date(turn.createdAt).toLocaleTimeString();
    const captionColor = isAssistant
      ? "rgba(255,255,255,0.72)"
      : "text.secondary";

    let backgroundColor = "background.paper";
    if (isAssistant) {
      backgroundColor = "grey.900";
    } else if (isSystem) {
      backgroundColor = "warning.50";
    }

    let cardColor = "text.primary";
    if (isAssistant) {
      cardColor = "common.white";
    }

    let borderColor = "divider";
    if (isAssistant) {
      borderColor = "grey.800";
    }

    let pastedTextNode: React.ReactNode = null;
    if (turn.pastedText) {
      const pastedBackgroundColor = isAssistant
        ? "rgba(255,255,255,0.08)"
        : "grey.50";
      const pastedBorderColor = isAssistant
        ? "rgba(255,255,255,0.12)"
        : "divider";

      pastedTextNode = (
        <Paper
          variant="outlined"
          sx={{
            px: 1.5,
            py: 1.25,
            backgroundColor: pastedBackgroundColor,
            borderColor: pastedBorderColor,
          }}
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
            {turn.pastedText}
          </Typography>
        </Paper>
      );
    }

    return (
      <Paper
        key={turn.id}
        variant="outlined"
        sx={{
          alignSelf,
          maxWidth: { xs: "100%", md: "85%" },
          px: 2,
          py: 1.75,
          backgroundColor,
          color: cardColor,
          borderColor,
        }}
      >
        <Stack spacing={1.25}>
          <Stack direction="row" justifyContent="space-between" spacing={2}>
            <Typography
              variant="caption"
              sx={{
                textTransform: "uppercase",
                letterSpacing: "0.08em",
                color: captionColor,
              }}
            >
              {turn.role}
            </Typography>
            <Typography
              variant="caption"
              sx={{
                color: captionColor,
              }}
            >
              {createdTime}
            </Typography>
          </Stack>

          <Typography variant="body1" sx={{ whiteSpace: "pre-wrap" }}>
            {turn.content}
          </Typography>

          {pastedTextNode}
        </Stack>
      </Paper>
    );
  });

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
          Conversation
        </Typography>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          Chat-first operations
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Ask for records, inspect activity, or prepare a change. Structured
          results appear alongside the conversation.
        </Typography>
      </Box>

      <Stack
        spacing={2}
        sx={{
          flex: 1,
          overflow: "auto",
          px: 3,
          py: 3,
          backgroundColor: "background.default",
        }}
      >
        {emptyState}
        {turnCards}
      </Stack>

      <Box
        component="form"
        onSubmit={handleSubmit}
        sx={{ px: 3, py: 2.5, borderTop: 1, borderColor: "divider" }}
      >
        <Stack spacing={2}>
          <TextField
            label="Message"
            multiline
            minRows={4}
            value={message}
            onChange={(event) => onMessageChange(event.target.value)}
            disabled={isBusy}
            placeholder="Find orders for Acme yesterday. Then update order 12 status to Complete."
          />

          <TextField
            label="Pasted note or email"
            multiline
            minRows={4}
            value={pastedText}
            onChange={(event) => onPastedTextChange(event.target.value)}
            disabled={isBusy}
            placeholder="Optional pasted text to help create or update a record."
          />

          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            spacing={2}
            alignItems={{ xs: "stretch", sm: "center" }}
          >
            <Typography variant="body2" color="text.secondary">
              Writes stay behind explicit confirmation.
            </Typography>

            <Button type="submit" variant="contained" disabled={isSendDisabled}>
              {sendButtonLabel}
            </Button>
          </Stack>
        </Stack>
      </Box>
    </Paper>
  );
};

export default AgentConversationPane;
