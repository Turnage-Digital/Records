import { ColumnType } from "./column-type";

export interface Column {
  name: string;
  property?: string;
  type: ColumnType;
}
