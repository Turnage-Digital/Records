import * as React from "react";

import { History } from "@mui/icons-material";
import { useSuspenseQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import useDetailPanel from "../components/detail-panel/use-detail-panel";
import RecordCard from "../components/record-card";
import Titlebar from "../components/titlebar";
import {
  editRecordPath,
  recordDetailsPath,
  recordsetRecordsPath,
  recordsetsPath,
} from "../lib/routes";
import {
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
} from "../query-options";

const RecordHistoryDrawer = React.lazy(
  () => import("../components/history/record-history-drawer"),
);

const RecordDetailsPage = () => {
  const { recordsetId, recordId } = useParams<{
    recordsetId: string;
    recordId: string;
  }>();
  if (!recordsetId || !recordId) {
    throw new Error("Recordset id and record id are required");
  }

  const navigate = useNavigate();
  const { openDetailPanel } = useDetailPanel();

  const recordsetDefinitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );

  const recordQuery = useSuspenseQuery(
    recordQueryOptions(recordsetId, Number(recordId)),
  );

  const definition = recordsetDefinitionQuery.data;
  const record = recordQuery.data;

  const handleNavigateToRecordsets = () => {
    navigate(recordsetsPath());
  };

  const handleNavigateToRecordset = () => {
    navigate(recordsetRecordsPath(recordsetId));
  };

  const handleShowHistory = () => {
    openDetailPanel(
      "Record history",
      <RecordHistoryDrawer
        recordsetId={recordsetId}
        recordId={Number(recordId)}
      />,
    );
  };

  const handleEditRecord = (
    currentRecordsetId: string,
    currentRecordId: number,
  ) => {
    navigate(editRecordPath(currentRecordsetId, currentRecordId));
  };

  const handleViewRecord = (
    currentRecordsetId: string,
    currentRecordId: number,
  ) => {
    navigate(recordDetailsPath(currentRecordsetId, currentRecordId));
  };

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
    {
      title: definition.name || "",
      onClick: handleNavigateToRecordset,
    },
  ];

  const actions = [
    {
      title: "Show history",
      icon: <History />,
      variant: "outlined" as const,
      color: "secondary" as const,
      onClick: handleShowHistory,
    },
  ];

  return (
    <>
      <Titlebar
        title={`ID ${record.id}`}
        breadcrumbs={breadcrumbs}
        actions={actions}
      />

      <RecordCard
        record={record}
        definition={definition}
        onEditRecord={handleEditRecord}
        onViewRecord={handleViewRecord}
      />
    </>
  );
};

export default RecordDetailsPage;
