import * as React from "react";

import { Paper } from "@mui/material";
import {
  DataGrid,
  type GridPaginationModel,
  type GridSortModel,
} from "@mui/x-data-grid";

import { getGridColDefs } from "../col-defs";

import type { RecordsetItemDefinition } from "../../models/recordset-item-definition";
import type { RecordsetPagedRecords } from "../../models/recordset-paged-records";

interface RecordsDesktopViewProps {
  data: RecordsetPagedRecords;
  definition: RecordsetItemDefinition;
  paginationModel: GridPaginationModel;
  sortModel: GridSortModel;
  onPaginationChange: (model: GridPaginationModel) => Promise<void> | void;
  onSortChange: (model: GridSortModel) => Promise<void> | void;
  onViewRecord: (recordsetId: string, recordId: number) => Promise<void> | void;
  onEditRecord: (recordsetId: string, recordId: number) => Promise<void> | void;
  onDeleteRecord: (
    recordsetId: string,
    recordId: number,
  ) => Promise<void> | void;
}

const RecordsDesktopView = ({
  data,
  definition,
  paginationModel,
  sortModel,
  onPaginationChange,
  onSortChange,
  onViewRecord,
  onEditRecord,
  onDeleteRecord,
}: RecordsDesktopViewProps) => {
  const gridColDefs = getGridColDefs(
    definition,
    onViewRecord,
    onEditRecord,
    onDeleteRecord,
  );

  const rows = data.items.map((item) => ({
    id: item.id,
    ...item.bag,
  }));

  return (
    <Paper>
      <DataGrid
        columns={gridColDefs}
        rows={rows}
        getRowId={(row) => row.id}
        rowCount={data.count}
        paginationMode="server"
        pageSizeOptions={[10, 25, 50]}
        onPaginationModelChange={onPaginationChange}
        sortingMode="server"
        onSortModelChange={onSortChange}
        initialState={{
          pagination: {
            paginationModel,
          },
          sorting: {
            sortModel,
          },
        }}
        disableColumnFilter
        disableColumnSelector
        disableRowSelectionOnClick
        sx={{
          border: "none",
          backgroundColor: "background.paper",
          "& .MuiDataGrid-columnHeaders": {
            backgroundColor: "grey.100",
            borderBottomColor: "divider",
          },
          "& .MuiDataGrid-cell": {
            borderBottomColor: "divider",
          },
        }}
      />
    </Paper>
  );
};

export default RecordsDesktopView;
