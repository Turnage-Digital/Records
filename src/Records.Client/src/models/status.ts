export interface Status {
  name: string;
  color: string;
}

export const getStatusFromName = (
  statuses: Status[],
  name: string,
): Status | undefined => {
  return statuses.find((status) => status.name === name);
};
