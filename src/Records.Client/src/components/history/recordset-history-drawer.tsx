import * as React from "react";

import { useInfiniteQuery } from "@tanstack/react-query";

import HistoryDrawer from "./history-drawer";
import { recordsetHistoryInfiniteQueryOptions } from "../../query-options";

interface RecordsetHistoryDrawerProps {
  recordsetId: string;
}

const RecordsetHistoryDrawer = ({
  recordsetId,
}: RecordsetHistoryDrawerProps) => {
  const query = useInfiniteQuery(
    recordsetHistoryInfiniteQueryOptions(recordsetId),
  );

  return <HistoryDrawer subtitle="Latest recordset updates" query={query} />;
};

export default RecordsetHistoryDrawer;
