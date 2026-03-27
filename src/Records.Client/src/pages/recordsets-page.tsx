import * as React from "react";

import { AddCircle } from "@mui/icons-material";
import { Grid } from "@mui/material";
import {
  useMutation,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";

import ConfirmDeleteDialog from "../components/confirm-delete-dialog";
import RecordsetCard from "../components/recordset-card";
import Titlebar from "../components/titlebar";
import { recordsetNamesQueryOptions } from "../query-options";

import type { RecordsetName } from "../models";

const RecordsetsPage = () => {
  const navigate = useNavigate();
  const [recordsetToDelete, setRecordsetToDelete] =
    React.useState<RecordsetName | null>(null);
  const queryClient = useQueryClient();

  const recordsetNamesQuery = useSuspenseQuery(recordsetNamesQueryOptions());

  const deleteRecordsetDialogMessage = recordsetToDelete
    ? `Are you sure you want to delete "${recordsetToDelete.name}"? This action cannot be undone.`
    : "Are you sure you want to delete this recordset? This action cannot be undone.";

  const deleteRecordsetMutation = useMutation({
    mutationFn: async (recordsetId: string) => {
      const request = new Request(`/api/recordsets/${recordsetId}`, {
        method: "DELETE",
      });
      const response = await fetch(request);
      if (!response.ok) {
        const message = await response
          .text()
          .catch(() => "Failed to delete recordset");
        throw new Error(message);
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["recordset-names"],
      });
    },
  });

  const handleDeleteClicked = (recordset: RecordsetName) => {
    setRecordsetToDelete(recordset);
  };

  const handleConfirmDelete = async () => {
    if (!recordsetToDelete) {
      return;
    }
    try {
      await deleteRecordsetMutation.mutateAsync(recordsetToDelete.id);
    } finally {
      setRecordsetToDelete(null);
    }
  };

  const handleCancelDelete = () => {
    setRecordsetToDelete(null);
  };

  const handleCreateRecordset = () => {
    navigate("/create");
  };

  const actions = [
    {
      title: "Create a Recordset",
      icon: <AddCircle />,
      onClick: handleCreateRecordset,
    },
  ];

  return (
    <>
      <Titlebar title="Recordsets" actions={actions} />

      <Grid container spacing={3}>
        {recordsetNamesQuery.data.map((recordsetName) => (
          <Grid key={recordsetName.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <RecordsetCard
              recordsetName={recordsetName}
              onDeleteClick={handleDeleteClicked}
            />
          </Grid>
        ))}
      </Grid>

      <ConfirmDeleteDialog
        open={Boolean(recordsetToDelete)}
        title="Delete recordset"
        description={deleteRecordsetDialogMessage}
        confirmDisabled={deleteRecordsetMutation.isPending}
        onCancel={handleCancelDelete}
        onConfirm={handleConfirmDelete}
      />
    </>
  );
};

export default RecordsetsPage;
