import * as React from "react";

import { Alert, Box, Grid, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams } from "react-router-dom";

import AgentConversationPane from "../components/agents/agent-conversation-pane";
import AgentWorkSurface from "../components/agents/agent-work-surface";
import { connectAgentThreadStream } from "../lib/agent-events";
import {
  confirmAgentProposal,
  postAgentTurn,
  rejectAgentProposal,
} from "../lib/agents";
import { agentThreadQueryOptions } from "../query-options";

const AgentThreadPage = () => {
  const { threadId } = useParams<{ threadId: string }>();
  const queryClient = useQueryClient();
  const [message, setMessage] = React.useState("");
  const [pastedText, setPastedText] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);

  const threadQuery = useQuery(agentThreadQueryOptions(threadId));

  React.useEffect(() => {
    if (!threadId) {
      return undefined;
    }

    return connectAgentThreadStream(threadId, () => {
      Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["agent-thread", threadId],
        }),
        queryClient.invalidateQueries({ queryKey: ["agent-threads"] }),
      ]).catch(() => undefined);
    });
  }, [queryClient, threadId]);

  const sendTurnMutation = useMutation({
    mutationFn: async () => {
      if (!threadId) {
        throw new Error("Thread id is required.");
      }

      const nextMessage =
        message.trim().length > 0
          ? message.trim()
          : "Create a draft from the pasted note.";
      const nextPastedText =
        pastedText.trim().length > 0 ? pastedText.trim() : undefined;

      return postAgentTurn(threadId, nextMessage, nextPastedText);
    },
    onSuccess: async (thread) => {
      queryClient.setQueryData(["agent-thread", thread.id], thread);
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
      setMessage("");
      setPastedText("");
    },
    onError: (nextError) => {
      setError(
        nextError instanceof Error
          ? nextError.message
          : "Failed to send the message.",
      );
    },
  });

  const confirmProposalMutation = useMutation({
    mutationFn: async (proposalId: string) => {
      if (!threadId) {
        throw new Error("Thread id is required.");
      }

      return confirmAgentProposal(threadId, proposalId);
    },
    onSuccess: async (thread) => {
      queryClient.setQueryData(["agent-thread", thread.id], thread);
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
    },
    onError: (nextError) => {
      setError(
        nextError instanceof Error
          ? nextError.message
          : "Failed to confirm the proposal.",
      );
    },
  });

  const rejectProposalMutation = useMutation({
    mutationFn: async (proposalId: string) => {
      if (!threadId) {
        throw new Error("Thread id is required.");
      }

      return rejectAgentProposal(threadId, proposalId);
    },
    onSuccess: async (thread) => {
      queryClient.setQueryData(["agent-thread", thread.id], thread);
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
    },
    onError: (nextError) => {
      setError(
        nextError instanceof Error
          ? nextError.message
          : "Failed to reject the proposal.",
      );
    },
  });

  if (threadQuery.isPending) {
    return (
      <Box
        sx={{
          minHeight: 320,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Typography color="text.secondary">Loading conversation...</Typography>
      </Box>
    );
  }

  if (threadQuery.isError) {
    const errorMessage =
      threadQuery.error instanceof Error
        ? threadQuery.error.message
        : "Unable to load this conversation.";

    return <Alert severity="error">{errorMessage}</Alert>;
  }

  const thread = threadQuery.data;
  const isBusy =
    sendTurnMutation.isPending ||
    confirmProposalMutation.isPending ||
    rejectProposalMutation.isPending;
  const updatedAtLabel = `Updated ${new Date(thread.updatedAt).toLocaleString()}`;
  const errorAlert = error ? <Alert severity="error">{error}</Alert> : null;

  const handleSubmit = () => {
    if (message.trim().length === 0 && pastedText.trim().length === 0) {
      return;
    }

    sendTurnMutation.mutate();
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography
          variant="overline"
          sx={{ color: "text.secondary", letterSpacing: "0.14em" }}
        >
          Agents
        </Typography>
        <Typography
          variant="h4"
          sx={{ fontWeight: 800, letterSpacing: "-0.04em" }}
        >
          {thread.title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {updatedAtLabel}
        </Typography>
      </Box>

      {errorAlert}

      <Grid container spacing={3} alignItems="stretch">
        <Grid size={{ xs: 12, xl: 7 }}>
          <AgentConversationPane
            turns={thread.turns}
            message={message}
            pastedText={pastedText}
            isBusy={isBusy}
            onMessageChange={setMessage}
            onPastedTextChange={setPastedText}
            onSubmit={handleSubmit}
          />
        </Grid>

        <Grid size={{ xs: 12, xl: 5 }}>
          <AgentWorkSurface
            artifact={thread.currentArtifact}
            proposal={thread.pendingProposal}
            isBusy={isBusy}
            onConfirm={(proposalId) =>
              confirmProposalMutation.mutate(proposalId)
            }
            onReject={(proposalId) => rejectProposalMutation.mutate(proposalId)}
          />
        </Grid>
      </Grid>
    </Stack>
  );
};

export default AgentThreadPage;
