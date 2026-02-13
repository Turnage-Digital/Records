export interface ChangeFeedMessage<T = unknown> {
  // Fully-qualified CLR type on server
  type: string;
  data: T;
  // ISO timestamp
  occurredOn: string;
}

export type ChangeFeedHandler = (message: ChangeFeedMessage) => void;
