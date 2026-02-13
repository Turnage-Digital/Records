import * as React from "react";

import {
  useMutation,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import { useAuth } from "../auth";
import { RecordEditor, Titlebar } from "../components";
import { resolveActorUlid } from "../lib/identifiers";
import { RecordItem } from "../models";
import {
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
} from "../query-options";

const EditRecordPage = () => {
  const auth = useAuth();
  const { recordsetId, recordId } = useParams<{
    recordsetId: string;
    recordId: string;
  }>();
  if (!recordsetId || !recordId) {
    throw new Error("Recordset id and record id are required");
  }

  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const definitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );
  const recordQuery = useSuspenseQuery(
    recordQueryOptions(recordsetId, Number(recordId)),
  );

  const definition = definitionQuery.data;
  const record = recordQuery.data;

  const [formState, setFormState] = React.useState<RecordItem>({
    id: record.id,
    recordsetId,
    bag: record.bag ?? {},
  });

  React.useEffect(() => {
    setFormState({
      id: record.id,
      recordsetId,
      bag: { ...record.bag },
    });
  }, [record.id, record.bag, recordsetId]);

  const updateRecordMutation = useMutation({
    mutationFn: async (recordPayload: RecordItem) => {
      const actorId = resolveActorUlid(auth.user);
      const request = new Request(
        `/api/recordsets/${recordsetId}/records/${recordId}`,
        {
          headers: {
            "Content-Type": "application/json",
          },
          method: "PUT",
          body: JSON.stringify({
            recordsetId,
            recordId: Number(recordId),
            bag: recordPayload.bag,
            updatedBy: actorId,
            updatedAt: new Date().toISOString(),
          }),
        },
      );
      const response = await fetch(request);
      if (!response.ok) {
        throw new Error("Failed to update record");
      }
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["recordset-records"],
          exact: false,
        }),
        queryClient.invalidateQueries({
          queryKey: ["record", recordsetId, Number(recordId)],
        }),
      ]);
      handleNavigateToRecordDetails();
    },
  });

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const handleNavigateToRecordset = () => {
    navigate(`/${recordsetId}`);
  };

  const handleNavigateToRecordDetails = () => {
    navigate(`/${recordsetId}/${recordId}`);
  };

  const handleBagChange = (nextBag: Record<string, unknown>) => {
    setFormState((prev) => ({ ...prev, bag: nextBag }));
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await updateRecordMutation.mutateAsync(formState);
  };

  const handleCancel = () => {
    navigate(-1);
  };

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
    {
      title: definition.name,
      onClick: handleNavigateToRecordset,
    },
    {
      title: `ID ${record.id}`,
      onClick: handleNavigateToRecordDetails,
    },
  ];

  const isSubmitting = updateRecordMutation.isPending;

  return (
    <>
      <Titlebar title={`Edit Record ${record.id}`} breadcrumbs={breadcrumbs} />
      <RecordEditor
        definition={definition}
        bag={formState.bag}
        onBagChange={handleBagChange}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        onCancel={handleCancel}
      />
    </>
  );
};

export default EditRecordPage;
