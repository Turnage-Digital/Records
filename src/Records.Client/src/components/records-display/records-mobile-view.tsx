import * as React from "react";

import { Box, Grid, Pagination, Stack } from "@mui/material";

import { RecordItem, RecordsetItemDefinition } from "../../models";
import RecordCard from "../record-card";

interface Props {
  records: RecordItem[];
  definition: RecordsetItemDefinition;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => Promise<void> | void;
  onViewRecord?: (
    recordsetId: string,
    recordId: number,
  ) => Promise<void> | void;
  onEditRecord?: (
    recordsetId: string,
    recordId: number,
  ) => Promise<void> | void;
  onDeleteRecord?: (
    recordsetId: string,
    recordId: number,
  ) => Promise<void> | void;
}

const RecordsMobileView = ({
  records,
  definition,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  onViewRecord,
  onEditRecord,
  onDeleteRecord,
}: Props) => {
  const totalPages = Math.ceil(totalCount / pageSize);
  const showPagination = totalPages > 1;
  return (
    <Stack spacing={{ xs: 3, md: 4 }}>
      <Grid container spacing={3}>
        {records.map((record) => (
          <Grid key={record.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <RecordCard
              record={record}
              definition={definition}
              onViewRecord={onViewRecord}
              onEditRecord={onEditRecord}
              onDeleteRecord={onDeleteRecord}
            />
          </Grid>
        ))}
      </Grid>

      {showPagination && (
        <Box sx={{ display: "flex", justifyContent: "center" }}>
          <Pagination
            count={totalPages}
            page={currentPage + 1}
            onChange={async (_event, page) => {
              await onPageChange(page - 1);
              const mainContent = document.querySelector("main");
              if (mainContent) {
                mainContent.scrollTo({ top: 0, behavior: "smooth" });
              }
            }}
            color="primary"
            size="large"
            showFirstButton
            showLastButton
            siblingCount={0}
          />
        </Box>
      )}
    </Stack>
  );
};

export default RecordsMobileView;
