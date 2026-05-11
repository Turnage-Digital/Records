export interface AgentThreadSummary {
  id: string;
  title: string;
  updatedAt: string;
}

export interface AgentTurn {
  id: string;
  role: string;
  content: string;
  pastedText?: string | null;
  createdAt: string;
  toolCalls: AgentToolCall[];
}

export interface AgentToolCall {
  id: string;
  name: string;
  argumentsJson: string;
  status: string;
  summary?: string | null;
  error?: string | null;
  startedAt: string;
  completedAt?: string | null;
}

export interface WorkspaceAttribute {
  key: string;
  label: string;
  type: string;
  value: unknown;
  displayValue?: string | null;
}

export interface WorkspaceSchemaField {
  key: string;
  label: string;
  type: string;
  required: boolean;
  allowedValues: string[];
  validationHint?: string | null;
}

export interface WorkspaceStateTransition {
  from: string;
  allowedNext: string[];
}

export interface WorkspaceEditorSchema {
  collectionId: string;
  collectionLabel: string;
  fields: WorkspaceSchemaField[];
  stateTransitions: WorkspaceStateTransition[];
}

export interface WorkspaceEntity {
  entityType: string;
  entityId: string;
  collectionId: string;
  displayName: string;
  attributes: WorkspaceAttribute[];
  schema?: WorkspaceEditorSchema | null;
}

export interface WorkspaceGridColumn {
  key: string;
  label: string;
  type: string;
}

export interface WorkspaceGridRow {
  entityId: string;
  displayName: string;
  attributes: WorkspaceAttribute[];
  availableActions: string[];
}

export interface WorkspaceGrid {
  collectionId: string;
  collectionLabel: string;
  resolvedFilters: string[];
  page: number;
  pageSize: number;
  totalCount: number;
  columns: WorkspaceGridColumn[];
  rows: WorkspaceGridRow[];
}

export interface WorkspaceProposalDiff {
  key: string;
  label: string;
  before: unknown;
  after: unknown;
}

export interface WorkspaceProposal {
  kind: string;
  proposalId: string;
  target: WorkspaceEntity;
  current: WorkspaceEntity;
  proposed: WorkspaceEntity;
  diffs: WorkspaceProposalDiff[];
  rationale: string;
  sourceExcerpt?: string | null;
  state: string;
  expiresAt: string;
}

export interface WorkspaceHistoryEntry {
  type: string;
  occurredAt: string;
  actorId?: string | null;
  attributes: WorkspaceAttribute[];
}

export interface WorkspaceHistory {
  entityId: string;
  entries: WorkspaceHistoryEntry[];
}

export interface WorkspaceNotification {
  id: string;
  title: string;
  body: string;
  occurredAt: string;
}

export interface WorkspaceNotifications {
  entityId: string;
  unreadCount: number;
  items: WorkspaceNotification[];
}

export interface WorkspaceArtifact {
  kind: string;
  title?: string | null;
  grid?: WorkspaceGrid | null;
  detail?: WorkspaceEntity | null;
  editor?: WorkspaceEditorSchema | null;
  history?: WorkspaceHistory | null;
  notifications?: WorkspaceNotifications | null;
}

export interface AgentThread {
  id: string;
  title: string;
  updatedAt: string;
  turns: AgentTurn[];
  currentArtifact?: WorkspaceArtifact | null;
  pendingProposal?: WorkspaceProposal | null;
}

export interface AgentStreamEvent {
  type: string;
  occurredAt: string;
  message?: string | null;
  artifact?: WorkspaceArtifact | null;
  proposal?: WorkspaceProposal | null;
  toolCall?: AgentToolCall | null;
}
