import { RecordItem } from "./record-item";

export interface RecordsetPagedRecords {
  id: string;
  count: number;
  items: RecordItem[];
}
