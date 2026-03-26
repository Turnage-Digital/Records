import * as React from "react";
import { useEffect, useMemo, useState } from "react";

import { ContentPaste } from "@mui/icons-material";
import {
  useMutation,
  useQueryClient,
  useSuspenseQuery,
} from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";

import { RecordEditor, SmartPasteDialog, Titlebar } from "../components";
import { extractRecordId } from "../lib/identifiers";
import { RecordItem } from "../models";
import { recordsetItemDefinitionQueryOptions } from "../query-options";

const CreateRecordPage = () => {
  const { recordsetId } = useParams<{ recordsetId: string }>();
  if (!recordsetId) {
    throw new Error("Recordset id is required");
  }

  const [smartPasteOpen, setSmartPasteOpen] = useState(false);

  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const recordsetDefinitionQuery = useSuspenseQuery(
    recordsetItemDefinitionQueryOptions(recordsetId),
  );

  const definition = recordsetDefinitionQuery.data;

  const [formState, setFormState] = useState<RecordItem>({
    id: null,
    recordsetId,
    bag: {},
  });

  const initialStatus = useMemo(
    () => definition.statuses[0]?.name,
    [definition.statuses],
  );

  useEffect(() => {
    setFormState({
      id: null,
      recordsetId,
      bag: initialStatus ? { status: initialStatus } : {},
    });
  }, [initialStatus, recordsetId, definition.id]);

  const createRecordMutation = useMutation({
    mutationFn: async (record: RecordItem) => {
      const request = new Request(`/api/recordsets/${recordsetId}/records`, {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify({
          recordsetId,
          bag: record.bag,
        }),
      });
      const response = await fetch(request);
      if (!response.ok) {
        throw new Error("Failed to create record");
      }

      const payload = (await response.json()) as unknown;
      const createdId = extractRecordId(payload);
      if (!createdId) {
        throw new Error("Record was created without an identifier.");
      }

      return {
        id: createdId,
        recordsetId,
        bag: record.bag,
      } satisfies RecordItem;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries();
    },
  });

  const handleBagChange = (nextBag: Record<string, unknown>) => {
    setFormState((prev) => ({ ...prev, bag: nextBag }));
  };

  const handleNavigateToRecordsets = () => {
    navigate("/");
  };

  const handleNavigateToRecordset = () => {
    navigate(`/${recordsetId}`);
  };

  const handleOpenSmartPaste = () => {
    setSmartPasteOpen(true);
  };

  const handleCloseSmartPaste = () => {
    setSmartPasteOpen(false);
  };

  const handlePaste = async (text: string) => {
    const command = { text };

    const postRequest = new Request(
      `/api/recordsets/${recordsetDefinitionQuery.data.id}/records/convert-text-to-record`,
      {
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
        body: JSON.stringify(command),
      },
    );

    const response = await fetch(postRequest);
    const json: RecordItem = await response.json();

    setFormState((prev) => ({ ...prev, bag: { ...prev.bag, ...json.bag } }));
    handleCloseSmartPaste();
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const mutated = await createRecordMutation.mutateAsync(formState);
    navigate(`/${recordsetId}/${mutated.id}`);
  };

  const actions = [
    {
      title: "Smart Paste",
      icon: <ContentPaste />,
      onClick: handleOpenSmartPaste,
    },
  ];

  const breadcrumbs = [
    {
      title: "Recordsets",
      onClick: handleNavigateToRecordsets,
    },
    {
      title: recordsetDefinitionQuery.data.name,
      onClick: handleNavigateToRecordset,
    },
  ];

  return (
    <>
      <Titlebar
        title="Create a Record"
        actions={actions}
        breadcrumbs={breadcrumbs}
      />
      <RecordEditor
        definition={definition}
        bag={formState.bag}
        onBagChange={handleBagChange}
        onSubmit={handleSubmit}
        isSubmitting={createRecordMutation.isPending}
      />
      <SmartPasteDialog
        open={smartPasteOpen}
        onClose={handleCloseSmartPaste}
        onPaste={handlePaste}
      />
    </>
  );
};

export default CreateRecordPage;
