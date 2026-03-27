import type { ColumnType } from "./column-type";

export interface MigrationPlan {
  changeColumnTypes?: ChangeColumnTypeOp[];
  removeColumns?: RemoveColumnOp[];
  tightenConstraints?: TightenConstraintsOp[];
  renameStorageKeys?: RenameStorageKeyOp[];
  removeStatuses?: RemoveStatusOp[];
}

export interface ChangeColumnTypeOp {
  key: string;
  targetType: ColumnType;
  converter: string;
}

export interface RemoveColumnOp {
  key: string;
  policy: string;
}

export interface TightenConstraintsOp {
  key: string;
  required?: boolean | null;
  allowedValues?: string[] | null;
  minNumber?: number | null;
  maxNumber?: number | null;
  regex?: string | null;
}

export interface RenameStorageKeyOp {
  from: string;
  to: string;
}

export interface RemoveStatusOp {
  name: string;
  mapTo?: string | null;
}
