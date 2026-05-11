import type { AgentThread, AgentThreadSummary } from "../models/agent";

async function throwIfNotOk(response: Response, fallbackMessage: string) {
  if (response.ok) {
    return;
  }

  const text = await response.text().catch(() => "");
  throw new Error(text || fallbackMessage);
}

export async function createAgentThread(
  title?: string,
): Promise<AgentThreadSummary> {
  const response = await fetch("/api/agents/threads", {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(title ? { title } : {}),
  });

  await throwIfNotOk(response, "Failed to create thread.");
  return (await response.json()) as AgentThreadSummary;
}

export async function deleteAgentThread(threadId: string): Promise<void> {
  const response = await fetch(`/api/agents/threads/${threadId}`, {
    method: "DELETE",
    credentials: "include",
  });

  await throwIfNotOk(response, "Failed to delete thread.");
}

export async function postAgentTurn(
  threadId: string,
  message: string,
  pastedText?: string,
): Promise<AgentThread> {
  const response = await fetch(`/api/agents/threads/${threadId}/turns`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ message, pastedText }),
  });

  await throwIfNotOk(response, "Failed to post turn.");
  return (await response.json()) as AgentThread;
}

export async function confirmAgentProposal(
  threadId: string,
  proposalId: string,
): Promise<AgentThread> {
  const response = await fetch(
    `/api/agents/threads/${threadId}/proposals/${proposalId}/confirm`,
    {
      method: "POST",
      credentials: "include",
    },
  );

  await throwIfNotOk(response, "Failed to confirm proposal.");
  return (await response.json()) as AgentThread;
}

export async function rejectAgentProposal(
  threadId: string,
  proposalId: string,
): Promise<AgentThread> {
  const response = await fetch(
    `/api/agents/threads/${threadId}/proposals/${proposalId}/reject`,
    {
      method: "POST",
      credentials: "include",
    },
  );

  await throwIfNotOk(response, "Failed to reject proposal.");
  return (await response.json()) as AgentThread;
}
