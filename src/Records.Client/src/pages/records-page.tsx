import * as React from "react";

import { AddCircle, History, NotificationsActive } from "@mui/icons-material";
import {
  Box,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  useMediaQuery,
  useTheme,
} from "@mui/material";
import { GridPaginationModel, GridSortModel } from "@mui/x-data-grid";
import {
  useMutation,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";

import {
  ConfirmDeleteDialog,
  NotificationsDrawer,
  RecordsDesktopView,
  RecordsetHistoryDrawer,
  RecordsMobileView,
  Titlebar,
  useSideDrawer,
} from "../components";
import { RecordsetSearch } from "../models";
import {
  pagedRecordsQueryOptions,
  recordsetItemDefinitionQueryOptions,
} from "../query-options";

export const getRecordsetSearch = (
  params: URLSearchParams,
): RecordsetSearch => {
  const page = Number(params.get("page") ?? "0");
  const pageSize = Number(params.get("pageSize") ?? "10");
  const status = params.get("status") ?? undefined;
  const field = params.get("field") ?? undefined;
  const sort = params.get("sort") ?? undefined;
  return {
    page,
    pageSize,
    status: status ?? undefined,
    field: field ?? undefined,
    sort: sort ?? undefined,
  };
};

const setRecordsetSearch = (
  updater: (current: RecordsetSearch) => RecordsetSearch,
  setParams: (nextInit: URLSearchParams) => void,
  currentParams: URLSearchParams,
) => {
  const nextSearch = updater(getRecordsetSearch(currentParams));
  const nextParams = new URLSearchParams();
  nextParams.set("page", nextSearch.page.toString());
  nextParams.set("pageSize", nextSearch.pageSize.toString());
  if (nextSearch.status) {
    nextParams.set("status", nextSearch.status);
  }
  if (nextSearch.field) {
    nextParams.set("field", nextSearch.field);
  }
  if (nextSearch.sort) {
    nextParams.set("sort", nextSearch.sort);
  }
  setParams(nextParams);
};

const RecordsPage = () => {
  const { recordsetId } = useParams<{ recordsetId: string }>();
  if (!recordsetId) {
    throw new Error("Recordset id is required");
  }

  const navigate = useNavigate();
  const { openDrawer } = useSideDrawer();
  const [searchParams, setSearchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const search = getRecordsetSearch(searchParams);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("lg"));
  const [recordToDelete, setRecordToDelete] = React.useState<{
    recordsetId: string;
    recordId: number;
  } | null>(null);

  const deleteRecordDialogMessage = recordToDelete
    ? `Are you sure you want to delete record #${recordToDelete.recordId}? This action cannot be undone.`
    : "Are you sure you want to delete this record? This action cannot be undone.";

  const recordsetDefinitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );

  const pagedRecordsQuery = useSuspenseQuery(
    pagedRecordsQueryOptions(search, recordsetId),
  );

  const definition = recordsetDefinitionQuery.data;

  const deleteRecordMutation = useMutation({
    mutationFn: async ({
      recordsetId: currentRecordsetId,
      recordId,
    }: {
      recordsetId: string;
      recordId: number;
    }) => {
      const request = new Request(
        `/api/recordsets/${currentRecordsetId}/records/${recordId}`,
        {
          method: "DELETE",
        },
      );
      const response = await fetch(request);
      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to delete record");
        throw new Error(message);
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries();
    },
  });

  const handlePaginationChange = (gridPaginationModel: GridPaginationModel) => {
    setRecordsetSearch(
      (prev) => ({
        ...prev,
        page: gridPaginationModel.page,
        pageSize: gridPaginationModel.pageSize,
      }),
      (next) => setSearchParams(next),
      searchParams,
    );
  };

  const handleSortChange = (gridSortModel: GridSortModel) => {
    setRecordsetSearch(
      (prev) => {
        if (gridSortModel.length === 0) {
          return { ...prev, field: undefined, sort: undefined };
        }
        const field = gridSortModel[0].field;
        const sort = gridSortModel[0].sort === "desc" ? "desc" : "asc";
        return { ...prev, field, sort };
      },
      (next) => setSearchParams(next),
      searchParams,
    );
  };

  const handleViewRecord = (currentRecordsetId: string, recordId: number) => {
    navigate(`/${currentRecordsetId}/${recordId}`);
  };

  const handleEditRecord = (currentRecordsetId: string, recordId: number) => {
    navigate(`/${currentRecordsetId}/${recordId}/edit`);
  };

  const handleDeleteRecord = (currentRecordsetId: string, recordId: number) => {
    setRecordToDelete({ recordsetId: currentRecordsetId, recordId });
  };

  const handleConfirmDeleteRecord = async () => {
    if (!recordToDelete) {
      return;
    }

    try {
      await deleteRecordMutation.mutateAsync(recordToDelete);
    } finally {
      setRecordToDelete(null);
    }
  };

  const handleCancelDeleteRecord = () => {
    setRecordToDelete(null);
  };

  const handleMobilePageChange = (newPage: number) => {
    setRecordsetSearch(
      (prev) => ({ ...prev, page: newPage }),
      (next) => setSearchParams(next),
      searchParams,
    );
  };

  const handleStatusFilterChange = (
    _event: React.MouseEvent<HTMLElement>,
    nextValue: string | null,
  ) => {
    if (!nextValue) {
      return;
    }

    setRecordsetSearch(
      (prev) => ({
        ...prev,
        page: 0,
        status: nextValue === "__all__" ? undefined : nextValue,
      }),
      (next) => setSearchParams(next),
      searchParams,
    );
  };

  const handleCreateRecord = () => {
    navigate(`/${recordsetId}/create`);
  };

  const handleShowHistory = () => {
    openDrawer(
      "Recordset history",
      <RecordsetHistoryDrawer recordsetId={recordsetId} />,
    );
  };

  const handleShowNotifications = () => {
    openDrawer(
      "Recordset notifications",
      <NotificationsDrawer
        recordsetId={recordsetId}
        contextLabel={recordsetDefinitionQuery.data.name}
      />,
    );
  };

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const paginationModel: GridPaginationModel = {
    page: search.page,
    pageSize: search.pageSize,
  };

  const sortModel: GridSortModel =
    search.field && search.sort
      ? [
          {
            field: search.field,
            sort: search.sort === "desc" ? "desc" : "asc",
          },
        ]
      : [];

  const actions = [
    {
      title: "Create a Record",
      icon: <AddCircle />,
      onClick: handleCreateRecord,
    },
    {
      title: "Show history",
      icon: <History />,
      variant: "outlined" as const,
      color: "secondary" as const,
      onClick: handleShowHistory,
    },
    {
      title: "Show notifications",
      icon: <NotificationsActive />,
      variant: "outlined" as const,
      color: "secondary" as const,
      onClick: handleShowNotifications,
    },
  ];

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
  ];

  const hasStatusFilter = definition.statuses.length > 0;
  const statusFilterValue = search.status ?? "__all__";
  const statusFilterBar = hasStatusFilter ? (
    <Box
      sx={{
        px: { xs: 0, md: 0.5 },
        pb: 2,
        display: "flex",
        alignItems: { xs: "flex-start", md: "center" },
        justifyContent: "space-between",
        gap: 1.5,
        flexWrap: "wrap",
      }}
    >
      <Typography variant="body2" color="text.secondary">
        Filter by status
      </Typography>
      <ToggleButtonGroup
        size="small"
        exclusive
        value={statusFilterValue}
        onChange={handleStatusFilterChange}
        aria-label="Record status filter"
      >
        <ToggleButton value="__all__">All</ToggleButton>
        {definition.statuses.map((status) => (
          <ToggleButton key={status.name} value={status.name}>
            {status.name}
          </ToggleButton>
        ))}
      </ToggleButtonGroup>
    </Box>
  ) : null;

  const recordsView = isMobile ? (
    <RecordsMobileView
      records={pagedRecordsQuery.data.items}
      definition={definition}
      totalCount={pagedRecordsQuery.data.count}
      currentPage={search.page}
      pageSize={search.pageSize}
      onPageChange={handleMobilePageChange}
      onViewRecord={handleViewRecord}
      onEditRecord={handleEditRecord}
      onDeleteRecord={handleDeleteRecord}
    />
  ) : (
    <RecordsDesktopView
      data={pagedRecordsQuery.data}
      definition={definition}
      paginationModel={paginationModel}
      sortModel={sortModel}
      onPaginationChange={handlePaginationChange}
      onSortChange={handleSortChange}
      onViewRecord={handleViewRecord}
      onEditRecord={handleEditRecord}
      onDeleteRecord={handleDeleteRecord}
    />
  );

  return (
    <>
      <Titlebar
        title={recordsetDefinitionQuery.data.name}
        actions={actions}
        breadcrumbs={breadcrumbs}
      />
      {statusFilterBar}
      {recordsView}
      <ConfirmDeleteDialog
        open={Boolean(recordToDelete)}
        title="Delete record"
        description={deleteRecordDialogMessage}
        confirmDisabled={deleteRecordMutation.isPending}
        onCancel={handleCancelDeleteRecord}
        onConfirm={handleConfirmDeleteRecord}
      />
    </>
  );
};

export default RecordsPage;
