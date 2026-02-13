export interface HistoryEntry<TType = string> {
  type: TType;
  on: string;
  by?: string | null;
  bag?: Record<string, unknown> | null;
}

export interface HistoryPage<TType = string> {
  items: HistoryEntry<TType>[];
  page: number;
  pageSize: number;
  total: number;
}
