import * as React from "react";

import BoltOutlinedIcon from "@mui/icons-material/BoltOutlined";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {
  Alert,
  Box,
  Button,
  Chip,
  Grid,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";

import { createAgentThread, postAgentTurn } from "../lib/agents";
import { agentThreadPath } from "../lib/routes";
import { agentThreadSummariesQueryOptions } from "../query-options";

const suggestedPrompts = [
  "Show me all orders for Acme yesterday",
  "Create an order for Acme with status Open",
  "Find yesterday's orders for Contoso and show the most recent one",
];

const AgentsHomePage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const threadsQuery = useQuery(agentThreadSummariesQueryOptions());

  const [message, setMessage] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);

  const startThreadMutation = useMutation({
    mutationFn: async ({
      message: nextMessage,
      startEmpty,
    }: {
      message?: string;
      startEmpty?: boolean;
    }) => {
      const thread = await createAgentThread();

      if (!startEmpty) {
        await postAgentTurn(thread.id, nextMessage?.trim() ?? "");
      }

      return thread.id;
    },
    onSuccess: async (threadId) => {
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
      setMessage("");
      navigate(agentThreadPath(threadId));
    },
    onError: (nextError) => {
      setError(
        nextError instanceof Error
          ? nextError.message
          : "Failed to start a new thread.",
      );
    },
  });

  const handleStartConversation = () => {
    if (message.trim().length === 0) {
      return;
    }

    startThreadMutation.mutate({ message });
  };

  const handleStartEmptyThread = () => {
    startThreadMutation.mutate({ startEmpty: true });
  };

  const handleMessageKeyDown = (event: React.KeyboardEvent) => {
    if (
      event.key === "Enter" &&
      !event.shiftKey &&
      !event.nativeEvent.isComposing
    ) {
      event.preventDefault();
      if (!startButtonDisabled) {
        handleStartConversation();
      }
    }
  };

  const errorAlert = error ? <Alert severity="error">{error}</Alert> : null;
  const startButtonDisabled =
    startThreadMutation.isPending || message.trim().length === 0;
  const startButtonLabel = startThreadMutation.isPending
    ? "Starting..."
    : "Start with this prompt";
  const recentThreads = threadsQuery.data?.slice(0, 6).map((thread) => {
    const updatedAtLabel = new Date(thread.updatedAt).toLocaleString();

    return (
      <Paper key={thread.id} variant="outlined" sx={{ px: 2, py: 1.5 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          justifyContent="space-between"
          spacing={1.5}
        >
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="subtitle2" noWrap>
              {thread.title}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {updatedAtLabel}
            </Typography>
          </Box>
          <Button
            endIcon={<OpenInNewIcon fontSize="small" />}
            onClick={() => navigate(agentThreadPath(thread.id))}
          >
            Open
          </Button>
        </Stack>
      </Paper>
    );
  });
  const emptyRecentThreadsState =
    Array.isArray(threadsQuery.data) && threadsQuery.data.length === 0 ? (
      <Paper
        variant="outlined"
        sx={{ px: 2, py: 2.5, backgroundColor: "grey.50" }}
      >
        <Typography variant="body2" color="text.secondary">
          No saved threads yet. Start one from the prompt box and it will appear
          here and in the sidebar.
        </Typography>
      </Paper>
    ) : null;

  return (
    <Stack spacing={3}>
      <Paper
        variant="outlined"
        sx={{
          p: { xs: 3, md: 4 },
          background:
            "linear-gradient(135deg, rgba(25,118,210,0.12) 0%, rgba(25,118,210,0.02) 48%, rgba(0,0,0,0) 100%)",
        }}
      >
        <Grid container spacing={3} alignItems="stretch">
          <Grid size={{ xs: 12, lg: 7 }}>
            <Stack spacing={2.5} sx={{ height: "100%" }}>
              <Box>
                <Typography
                  variant="overline"
                  sx={{ color: "text.secondary", letterSpacing: "0.14em" }}
                >
                  Agents
                </Typography>
                <Typography
                  variant="h3"
                  sx={{
                    fontWeight: 800,
                    letterSpacing: "-0.04em",
                    maxWidth: "14ch",
                  }}
                >
                  Put chat in front of the operational work.
                </Typography>
                <Typography
                  variant="body1"
                  color="text.secondary"
                  sx={{ maxWidth: 680, mt: 1.5 }}
                >
                  Ask for records in plain English, review structured results,
                  then confirm writes only when the draft looks right.
                </Typography>
              </Box>

              <Stack direction="row" flexWrap="wrap" gap={1}>
                {suggestedPrompts.map((prompt) => (
                  <Chip
                    key={prompt}
                    label={prompt}
                    onClick={() => setMessage(prompt)}
                    variant="outlined"
                  />
                ))}
              </Stack>

              <Stack spacing={2}>
                {errorAlert}

                <TextField
                  label="Start with a prompt"
                  multiline
                  minRows={5}
                  maxRows={12}
                  value={message}
                  onChange={(event) => setMessage(event.target.value)}
                  onKeyDown={handleMessageKeyDown}
                  placeholder="Ask for records, paste an email, or describe the change you want drafted."
                  helperText="Press Enter to start. Use Shift+Enter for a new line."
                  disabled={startThreadMutation.isPending}
                />

                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  spacing={1.5}
                  alignItems={{ xs: "stretch", sm: "center" }}
                >
                  <Button
                    variant="contained"
                    size="large"
                    startIcon={<BoltOutlinedIcon />}
                    onClick={handleStartConversation}
                    disabled={startButtonDisabled}
                  >
                    {startButtonLabel}
                  </Button>
                  <Button
                    variant="outlined"
                    size="large"
                    onClick={handleStartEmptyThread}
                    disabled={startThreadMutation.isPending}
                  >
                    Start empty thread
                  </Button>
                </Stack>
              </Stack>
            </Stack>
          </Grid>

          <Grid size={{ xs: 12, lg: 5 }}>
            <Paper
              variant="outlined"
              sx={{
                height: "100%",
                p: 3,
                backgroundColor: "background.paper",
              }}
            >
              <Stack spacing={2.5}>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>
                    Pick up a recent conversation
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Saved threads stay scoped to the current user and tenant so
                    you can return to a conversation without losing context.
                  </Typography>
                </Box>

                <Stack spacing={1.25}>
                  {recentThreads}
                  {emptyRecentThreadsState}
                </Stack>
              </Stack>
            </Paper>
          </Grid>
        </Grid>
      </Paper>
    </Stack>
  );
};

export default AgentsHomePage;
