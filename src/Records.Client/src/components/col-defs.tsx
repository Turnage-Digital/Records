import * as React from "react";

import { Delete, Edit, Visibility } from "@mui/icons-material";
import { GridActionsCellItem, GridColDef } from "@mui/x-data-grid";

import {
  Column,
  ColumnType,
  getStatusFromName,
  RecordsetItemDefinition,
} from "../models";
import StatusChip from "./status-chip";

export const getGridColDefs = (
  recordsetDefinition: RecordsetItemDefinition,
  handleViewClicked: (recordsetId: string, recordId: number) => void,
  handleEditClicked: (recordsetId: string, recordId: number) => void,
  handleDeleteClicked: (recordsetId: string, recordId: number) => void,
): GridColDef[] => {
  const retval: GridColDef[] = [];

  retval.push({
    field: "id",
    headerName: "ID",
    width: 75,
    sortable: false,
    disableColumnMenu: true,
  });

  const mapped = recordsetDefinition.columns.map((column: Column) => {
    const retval: GridColDef = {
      field: column.property!,
      headerName: column.name,
      flex: 1,
    };

    if (column.type === ColumnType.Date) {
      retval.valueFormatter = (params) => {
        const date = new Date(params);
        const retval = date.toLocaleDateString();
        return retval;
      };
    }
    return retval;
  });

  retval.push(...mapped);

  retval.push({
    field: "status",
    headerName: "Status",
    width: 150,
    renderCell: (params) => (
      <StatusChip
        status={getStatusFromName(recordsetDefinition.statuses, params.value)}
      />
    ),
  });

  retval.push({
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

  return retval;
};
