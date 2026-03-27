import { ChangeFeedHandler, ChangeFeedMessage } from "../models";

export interface ConnectOptions {
  url?: string;
  withCredentials?: boolean;
  onError?: (error: Event) => void;
  onOpen?: (ev: Event) => void;
}

// Connects to the server-sent events change feed and invokes the handler for each message.
export function connectChangeFeed(
  onMessage: ChangeFeedHandler,
  options?: ConnectOptions,
): { close: () => void; source: EventSource } {
  const url = options?.url ?? "/api/changes/stream";
  const source = new EventSource(url, {
    withCredentials: options?.withCredentials ?? true,
  });

  source.onmessage = (event: MessageEvent<string>) => {
    try {
      const parsed = JSON.parse(event.data) as ChangeFeedMessage;
      onMessage(parsed);
    } catch {
      // Ignore malformed payloads
    }
  };

  if (options?.onError) source.onerror = options.onError;
  if (options?.onOpen) source.onopen = options.onOpen;

  return {
    close: () => source.close(),
    source,
  };
}

// Utility to route events by their "type" string to specific callbacks.
export function createChangeFeedRouter(
  routes: Record<string, (data: unknown, message: ChangeFeedMessage) => void>,
): ChangeFeedHandler {
  return (message) => {
    const handler = routes[message.type];
    if (typeof handler === "function") {
      handler(message.data, message);
    }
  };
}
