import type { AgentStreamEvent } from "../models/agent";

export function connectAgentThreadStream(
  threadId: string,
  onEvent: (event: AgentStreamEvent) => void,
): () => void {
  const source = new EventSource(`/api/agents/threads/${threadId}/stream`, {
    withCredentials: true,
  });

  source.onmessage = (message) => {
    try {
      onEvent(JSON.parse(message.data) as AgentStreamEvent);
    } catch {
      // Ignore malformed events from the stream.
    }
  };

  return () => source.close();
}
