import * as React from "react";

import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import AgentConversationPane from "../components/agents/agent-conversation-pane";
import AgentWorkSurface from "../components/agents/agent-work-surface";
import ConfirmDeleteDialog from "../components/confirm-delete-dialog";
import { connectAgentThreadStream } from "../lib/agent-events";
import {
  confirmAgentProposal,
  deleteAgentThread,
  postAgentTurn,
  rejectAgentProposal,
} from "../lib/agents";
import { agentsHomePath } from "../lib/routes";
import { agentThreadQueryOptions } from "../query-options";

import type {
  AgentStreamEvent,
  AgentThread,
  AgentToolCall,
  AgentTurn,
} from "../models/agent";

const draftUserTurnId = "draft-user-turn";
const draftAssistantTurnId = "draft-assistant-turn";

const createDraftTurn = (
  id: string,
  role: string,
  content: string,
  createdAt: string,
): AgentTurn => ({
  id,
  role,
  content,
  createdAt,
  toolCalls: [],
});

const upsertToolCall = (
  toolCalls: AgentToolCall[],
  nextToolCall: AgentToolCall,
): AgentToolCall[] => {
  const existingIndex = toolCalls.findIndex(
    (toolCall) => toolCall.id === nextToolCall.id,
  );

  if (existingIndex === -1) {
    return [...toolCalls, nextToolCall];
  }

  return toolCalls.map((toolCall, index) =>
    index === existingIndex ? { ...toolCall, ...nextToolCall } : toolCall,
  );
};

const updateDraftAssistantTurn = (
  turns: AgentTurn[],
  updater: (turn: AgentTurn) => AgentTurn,
): AgentTurn[] => {
  const existingDraft = turns.find((turn) => turn.id === draftAssistantTurnId);
  if (existingDraft) {
    return turns.map((turn) =>
      turn.id === draftAssistantTurnId ? updater(turn) : turn,
    );
  }

  return [
    ...turns,
    updater(
      createDraftTurn(
        draftAssistantTurnId,
        "assistant",
        "",
        new Date().toISOString(),
      ),
    ),
  ];
};

const applyAgentStreamEvent = (
  thread: AgentThread | undefined,
  streamEvent: AgentStreamEvent,
): AgentThread | undefined => {
  if (!thread) {
    return thread;
  }

  if (streamEvent.type === "assistant_message_delta" && streamEvent.message) {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      turns: updateDraftAssistantTurn(thread.turns, (turn) => ({
        ...turn,
        content: `${turn.content}${streamEvent.message ?? ""}`,
      })),
    };
  }

  if (streamEvent.type === "assistant_final_message") {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      turns: updateDraftAssistantTurn(thread.turns, (turn) => ({
        ...turn,
        content: streamEvent.message ?? turn.content,
      })),
    };
  }

  if (
    (streamEvent.type === "tool_call_started" ||
      streamEvent.type === "tool_call_completed" ||
      streamEvent.type === "tool_call_failed") &&
    streamEvent.toolCall
  ) {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      turns: updateDraftAssistantTurn(thread.turns, (turn) => ({
        ...turn,
        toolCalls: upsertToolCall(turn.toolCalls, streamEvent.toolCall!),
      })),
    };
  }

  if (streamEvent.type === "artifact_replace") {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      currentArtifact: streamEvent.artifact ?? thread.currentArtifact,
    };
  }

  if (streamEvent.type === "proposal_created") {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      pendingProposal: streamEvent.proposal ?? thread.pendingProposal,
    };
  }

  if (
    streamEvent.type === "proposal_applied" ||
    streamEvent.type === "proposal_expired"
  ) {
    return {
      ...thread,
      updatedAt: streamEvent.occurredAt,
      pendingProposal: null,
    };
  }

  return thread;
};

const AgentThreadPage = () => {
  const { threadId } = useParams<{ threadId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [message, setMessage] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);

  const threadQuery = useQuery(agentThreadQueryOptions(threadId));

  React.useEffect(() => {
    if (!threadId) {
      return undefined;
    }

    return connectAgentThreadStream(threadId, (streamEvent) => {
      queryClient.setQueryData<AgentThread | undefined>(
        ["agent-thread", threadId],
        (currentThread) => applyAgentStreamEvent(currentThread, streamEvent),
      );

      if (
        streamEvent.type === "assistant_final_message" ||
        streamEvent.type === "proposal_created" ||
        streamEvent.type === "proposal_applied" ||
        streamEvent.type === "proposal_expired"
      ) {
        queryClient
          .invalidateQueries({ queryKey: ["agent-threads"] })
          .catch(() => undefined);
      }
    });
  }, [queryClient, threadId]);

  const sendTurnMutation = useMutation({
    mutationFn: async (submittedMessage: string) => {
      if (!threadId) {
        throw new Error("Thread id is required.");
      }

      return postAgentTurn(threadId, submittedMessage);
    },
    onMutate: async (submittedMessage) => {
      if (!threadId) {
        return { previousThread: undefined, previousMessage: message };
      }

      await queryClient.cancelQueries({ queryKey: ["agent-thread", threadId] });
      const previousThread = queryClient.getQueryData<AgentThread>([
        "agent-thread",
        threadId,
      ]);
      const submittedAt = new Date().toISOString();

      if (previousThread) {
        queryClient.setQueryData<AgentThread>(["agent-thread", threadId], {
          ...previousThread,
          updatedAt: submittedAt,
          turns: [
            ...previousThread.turns.filter(
              (turn) =>
                turn.id !== draftUserTurnId && turn.id !== draftAssistantTurnId,
            ),
            createDraftTurn(
              draftUserTurnId,
              "user",
              submittedMessage,
              submittedAt,
            ),
            createDraftTurn(
              draftAssistantTurnId,
              "assistant",
              "",
              new Date(Date.now() + 1).toISOString(),
            ),
          ],
        });
      }

      setMessage("");
      setError(null);

      return {
        previousThread,
        previousMessage: message,
      };
    },
    onSuccess: async (thread) => {
      queryClient.setQueryData(["agent-thread", thread.id], thread);
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
    },
    onError: (nextError, _submittedMessage, context) => {
      if (threadId && context?.previousThread) {
        queryClient.setQueryData(
          ["agent-thread", threadId],
          context.previousThread,
        );
      }

      if (context?.previousMessage) {
        setMessage(context.previousMessage);
      }

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

  const deleteThreadMutation = useMutation({
    mutationFn: async () => {
      if (!threadId) {
        throw new Error("Thread id is required.");
      }

      await deleteAgentThread(threadId);
    },
    onSuccess: async () => {
      if (!threadId) {
        return;
      }

      queryClient.removeQueries({
        queryKey: ["agent-thread", threadId],
        exact: true,
      });
      await queryClient.invalidateQueries({ queryKey: ["agent-threads"] });
      setError(null);
      setDeleteDialogOpen(false);
      navigate(agentsHomePath());
    },
    onError: (nextError) => {
      setError(
        nextError instanceof Error
          ? nextError.message
          : "Failed to delete the thread.",
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
    rejectProposalMutation.isPending ||
    deleteThreadMutation.isPending;
  const updatedAtLabel = `Updated ${new Date(thread.updatedAt).toLocaleString()}`;
  const errorAlert = error ? <Alert severity="error">{error}</Alert> : null;
  const deleteThreadDialogMessage = `Are you sure you want to delete "${thread.title}"? This action cannot be undone.`;

  const handleSubmit = () => {
    const submittedMessage = message.trim();
    if (submittedMessage.length === 0) {
      return;
    }

    sendTurnMutation.mutate(submittedMessage);
  };

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        gap: 2,
        flex: 1,
        minHeight: 0,
        overflow: "hidden",
      }}
    >
      <Stack
        direction={{ xs: "column", md: "row" }}
        justifyContent="space-between"
        spacing={1.5}
        sx={{ px: { xs: 0.5, md: 0 } }}
      >
        <Box>
          <Typography
            variant="h6"
            sx={{ fontWeight: 700, letterSpacing: "-0.02em" }}
          >
            {thread.title}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {updatedAtLabel}
          </Typography>
        </Box>

        <Button
          variant="outlined"
          color="error"
          startIcon={<DeleteOutlineIcon />}
          onClick={() => setDeleteDialogOpen(true)}
          disabled={deleteThreadMutation.isPending}
          sx={{ alignSelf: { xs: "flex-start", md: "center" } }}
        >
          Delete thread
        </Button>
      </Stack>

      {errorAlert}

      <Box
        sx={{
          display: "grid",
          gap: 2,
          flex: 1,
          minHeight: 0,
          overflow: "hidden",
          gridTemplateColumns: {
            xs: "1fr",
            lg: "minmax(0, 7fr) minmax(360px, 5fr)",
          },
          gridTemplateRows: {
            xs: "minmax(0, 1.4fr) minmax(220px, 0.9fr)",
            lg: "minmax(0, 1fr)",
          },
        }}
      >
        <Box sx={{ display: "flex", minWidth: 0, minHeight: 0 }}>
          <AgentConversationPane
            turns={thread.turns}
            message={message}
            isBusy={isBusy}
            onMessageChange={setMessage}
            onSubmit={handleSubmit}
          />
        </Box>

        <Box sx={{ display: "flex", minWidth: 0, minHeight: 0 }}>
          <AgentWorkSurface
            artifact={thread.currentArtifact}
            proposal={thread.pendingProposal}
            isBusy={isBusy}
            onConfirm={(proposalId) =>
              confirmProposalMutation.mutate(proposalId)
            }
            onReject={(proposalId) => rejectProposalMutation.mutate(proposalId)}
          />
        </Box>
      </Box>

      <ConfirmDeleteDialog
        open={deleteDialogOpen}
        title="Delete thread"
        description={deleteThreadDialogMessage}
        confirmDisabled={deleteThreadMutation.isPending}
        onCancel={() => setDeleteDialogOpen(false)}
        onConfirm={() => deleteThreadMutation.mutate()}
      />
    </Box>
  );
};

export default AgentThreadPage;
