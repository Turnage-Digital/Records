export const agentsHomePath = () => "/";

export const agentThreadPath = (threadId: string) => `/threads/${threadId}`;

export const recordsetsPath = () => "/recordsets";

export const createRecordsetPath = () => "/recordsets/create";

export const recordsetRecordsPath = (recordsetId: string) =>
  `/recordsets/${recordsetId}/records`;

export const createRecordPath = (recordsetId: string) =>
  `${recordsetRecordsPath(recordsetId)}/create`;

export const recordDetailsPath = (
  recordsetId: string,
  recordId: number | string,
) => `${recordsetRecordsPath(recordsetId)}/${recordId}`;

export const editRecordPath = (
  recordsetId: string,
  recordId: number | string,
) => `${recordDetailsPath(recordsetId, recordId)}/edit`;

export const editRecordsetPath = (recordsetId: string) =>
  `/recordsets/${recordsetId}/edit`;
