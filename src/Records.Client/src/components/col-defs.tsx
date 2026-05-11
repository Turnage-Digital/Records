import * as React from "react";

import { Delete, Edit, Visibility } from "@mui/icons-material";
import { GridActionsCellItem, type GridColDef } from "@mui/x-data-grid";

import StatusChip from "./status-chip";
import { ColumnType } from "../models/column-type";
import { getStatusFromName } from "../models/status";

import type { Column } from "../models/column";
import type { RecordsetItemDefinition } from "../models/recordset-item-definition";

export const getGridColDefs = (
  recordsetDefinition: RecordsetItemDefinition,
  handleViewClicked: (recordsetId: string, recordId: number) => void,
  handleEditClicked: (recordsetId: string, recordId: number) => void,
  handleDeleteClicked: (recordsetId: string, recordId: number) => void,
): GridColDef[] => {
  const columnDefinitions: GridColDef[] = [];

  columnDefinitions.push({
    field: "id",
    headerName: "ID",
    width: 75,
    sortable: false,
    disableColumnMenu: true,
  });

  const mappedColumns = recordsetDefinition.columns.map((column: Column) => {
    const columnDefinition: GridColDef = {
      field: column.property!,
      headerName: column.name,
      flex: 1,
    };

    if (column.type === ColumnType.Date) {
      columnDefinition.valueFormatter = (params) => {
        const date = new Date(params);
        return date.toLocaleDateString();
      };
    }
    return columnDefinition;
  });

  columnDefinitions.push(...mappedColumns);

  columnDefinitions.push({
    field: "status",
    headerName: "Status",
    width: 150,
    renderCell: (params) => (
      <StatusChip
        status={getStatusFromName(recordsetDefinition.statuses, params.value)}
      />
    ),
  });

  columnDefinitions.push({
    field: "actions",
    type: "actions",
    headerName: "",
    width: 75,
    cellClassName: "actions",
    headerAlign: "center",
    align: "center",
    getActions: ({ id }) => {
      return [
        <GridActionsCellItem
          key={`${id}-view`}
          showInMenu
          icon={<Visibility color="primary" fontSize="small" />}
          label="View"
          onClick={() =>
            handleViewClicked(recordsetDefinition.id!, id as number)
          }
        />,
        <GridActionsCellItem
          key={`${id}-edit`}
          showInMenu
          icon={<Edit color="primary" fontSize="small" />}
          label="Edit"
          onClick={() =>
            handleEditClicked(recordsetDefinition.id!, id as number)
          }
        />,
        <GridActionsCellItem
          key={`${id}-delete`}
          showInMenu
          icon={<Delete color="error" fontSize="small" />}
          label="Delete"
          onClick={() =>
            handleDeleteClicked(recordsetDefinition.id!, id as number)
          }
        />,
      ];
    },
  });

  return columnDefinitions;
};
