import * as React from "react";

import { History } from "@mui/icons-material";
import { useSuspenseQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import {
  RecordCard,
  RecordHistoryDrawer,
  Titlebar,
  useSideDrawer,
} from "../components";
import {
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
} from "../query-options";

const RecordDetailsPage = () => {
  const { recordsetId, recordId } = useParams<{
    recordsetId: string;
    recordId: string;
  }>();
  if (!recordsetId || !recordId) {
    throw new Error("Recordset id and record id are required");
  }

  const navigate = useNavigate();
  const { openDrawer } = useSideDrawer();

  const recordsetDefinitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );

  const recordQuery = useSuspenseQuery(
    recordQueryOptions(recordsetId, Number(recordId)),
  );

  const definition = recordsetDefinitionQuery.data;
  const record = recordQuery.data;

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const handleNavigateToRecordset = () => {
    navigate(`/${recordsetId}`);
  };

  const handleShowHistory = () => {
    openDrawer(
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
    navigate(`/${currentRecordsetId}/${currentRecordId}/edit`);
  };

  const handleViewRecord = (
    currentRecordsetId: string,
    currentRecordId: number,
  ) => {
    navigate(`/${currentRecordsetId}/${currentRecordId}`);
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
