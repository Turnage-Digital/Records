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
  isBusy: boolean;
  onMessageChange: (value: string) => void;
  onSubmit: () => void;
}

function getLatestExchangeTurnIds(turns: AgentTurn[]) {
  let latestUserIndex = -1;
  for (let index = turns.length - 1; index >= 0; index -= 1) {
    if (turns[index]?.role === "user") {
      latestUserIndex = index;
      break;
    }
  }

  if (latestUserIndex === -1) {
    return null;
  }

  let latestAssistantIndex = -1;
  for (let index = latestUserIndex + 1; index < turns.length; index += 1) {
    if (turns[index]?.role === "assistant") {
      latestAssistantIndex = index;
      break;
    }
  }

  return {
    userTurnId: turns[latestUserIndex].id,
    assistantTurnId:
      latestAssistantIndex === -1 ? null : turns[latestAssistantIndex].id,
  };
}

const AgentConversationPane = ({
  turns,
  message,
  isBusy,
  onMessageChange,
  onSubmit,
}: AgentConversationPaneProps) => {
  const scrollContainerRef = React.useRef<HTMLDivElement | null>(null);
  const turnElementRefs = React.useRef(new Map<string, HTMLDivElement>());
  const hasInitializedScrollRef = React.useRef(false);
  const previousTurnCountRef = React.useRef(0);
  const previousLastTurnIdRef = React.useRef<string | null>(null);

  React.useLayoutEffect(() => {
    const scrollContainer = scrollContainerRef.current;
    const latestTurn = turns.length > 0 ? turns[turns.length - 1] : undefined;

    if (!scrollContainer || !latestTurn) {
      previousTurnCountRef.current = turns.length;
      previousLastTurnIdRef.current = latestTurn?.id ?? null;
      return;
    }

    if (!hasInitializedScrollRef.current) {
      scrollContainer.scrollTop = scrollContainer.scrollHeight;
      hasInitializedScrollRef.current = true;
      previousTurnCountRef.current = turns.length;
      previousLastTurnIdRef.current = latestTurn.id;
      return;
    }

    const hasNewTurn =
      turns.length !== previousTurnCountRef.current ||
      latestTurn.id !== previousLastTurnIdRef.current;

    if (!isBusy && !hasNewTurn) {
      return;
    }

    const latestExchangeTurnIds = getLatestExchangeTurnIds(turns);
    if (!latestExchangeTurnIds) {
      scrollContainer.scrollTop = scrollContainer.scrollHeight;
      previousTurnCountRef.current = turns.length;
      previousLastTurnIdRef.current = latestTurn.id;
      return;
    }

    const userTurnElement = turnElementRefs.current.get(
      latestExchangeTurnIds.userTurnId,
    );
    const assistantTurnElement = latestExchangeTurnIds.assistantTurnId
      ? turnElementRefs.current.get(latestExchangeTurnIds.assistantTurnId)
      : null;

    if (!userTurnElement) {
      previousTurnCountRef.current = turns.length;
      previousLastTurnIdRef.current = latestTurn.id;
      return;
    }

    const exchangeTop = Math.max(userTurnElement.offsetTop - 12, 0);
    const exchangeBottom = assistantTurnElement
      ? assistantTurnElement.offsetTop + assistantTurnElement.offsetHeight
      : userTurnElement.offsetTop + userTurnElement.offsetHeight;
    const exchangeHeight = exchangeBottom - exchangeTop;

    scrollContainer.scrollTop =
      exchangeHeight <= scrollContainer.clientHeight
        ? exchangeTop
        : Math.max(exchangeBottom - scrollContainer.clientHeight, 0);

    previousTurnCountRef.current = turns.length;
    previousLastTurnIdRef.current = latestTurn.id;
  }, [turns, isBusy]);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit();
  };

  const handleMessageKeyDown = (event: React.KeyboardEvent) => {
    if (
      event.key === "Enter" &&
      !event.shiftKey &&
      !event.nativeEvent.isComposing
    ) {
      event.preventDefault();
      if (!isSendDisabled) {
        onSubmit();
      }
    }
  };

  const isSendDisabled = isBusy || message.trim().length === 0;
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

    const toolCallsNode =
      turn.toolCalls.length === 0 ? null : (
        <Stack spacing={1}>
          {turn.toolCalls.map((toolCall) => {
            let toolColor = captionColor;
            if (toolCall.status === "failed") {
              toolColor = isAssistant ? "error.light" : "error.main";
            }
            const toolBackgroundColor = isAssistant
              ? "rgba(255,255,255,0.06)"
              : "grey.50";
            const toolBorderColor = isAssistant
              ? "rgba(255,255,255,0.12)"
              : "divider";
            const toolTextColor = isAssistant
              ? "rgba(255,255,255,0.9)"
              : "text.primary";
            const summaryNode = toolCall.summary ? (
              <Typography variant="body2" sx={{ color: toolTextColor }}>
                {toolCall.summary}
              </Typography>
            ) : null;
            const errorNode = toolCall.error ? (
              <Typography
                variant="body2"
                sx={{ color: isAssistant ? "error.light" : "error.main" }}
              >
                {toolCall.error}
              </Typography>
            ) : null;

            return (
              <Paper
                key={toolCall.id}
                variant="outlined"
                sx={{
                  px: 1.5,
                  py: 1.25,
                  backgroundColor: toolBackgroundColor,
                  borderColor: toolBorderColor,
                  color: toolTextColor,
                }}
              >
                <Stack spacing={0.75}>
                  <Stack
                    direction="row"
                    justifyContent="space-between"
                    spacing={2}
                  >
                    <Typography variant="caption" sx={{ color: toolColor }}>
                      {toolCall.name}
                    </Typography>
                    <Typography variant="caption" sx={{ color: toolColor }}>
                      {toolCall.status}
                    </Typography>
                  </Stack>

                  {summaryNode}
                  {errorNode}
                </Stack>
              </Paper>
            );
          })}
        </Stack>
      );

    return (
      <Box
        key={turn.id}
        ref={(element: HTMLDivElement | null) => {
          if (element) {
            turnElementRefs.current.set(turn.id, element);
            return;
          }

          turnElementRefs.current.delete(turn.id);
        }}
        sx={{
          alignSelf,
          maxWidth: { xs: "100%", md: "85%" },
        }}
      >
        <Paper
          variant="outlined"
          sx={{
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

            {toolCallsNode}
            {pastedTextNode}
          </Stack>
        </Paper>
      </Box>
    );
  });

  return (
    <Paper
      variant="outlined"
      sx={{
        display: "flex",
        flexDirection: "column",
        minHeight: 0,
        height: "100%",
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
        ref={scrollContainerRef}
        spacing={2}
        sx={{
          flex: 1,
          minHeight: 0,
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
        sx={{
          px: 3,
          py: 2.5,
          mt: "auto",
          borderTop: 1,
          borderColor: "divider",
          backgroundColor: "background.paper",
          position: "sticky",
          bottom: 0,
          zIndex: 1,
          boxShadow: "0 -12px 24px rgba(15, 23, 42, 0.08)",
        }}
      >
        <Stack spacing={2}>
          <TextField
            label="Message"
            multiline
            minRows={3}
            maxRows={10}
            value={message}
            onChange={(event) => onMessageChange(event.target.value)}
            onKeyDown={handleMessageKeyDown}
            disabled={isBusy}
            placeholder="Find orders for Acme yesterday. Paste an email if you want a draft. Ask for an update when you're ready."
            helperText="Press Enter to send. Use Shift+Enter for a new line."
          />

          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="flex-end"
            spacing={2}
            alignItems={{ xs: "stretch", sm: "center" }}
          >
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
