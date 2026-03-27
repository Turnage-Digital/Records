import * as React from "react";

import { useInfiniteQuery } from "@tanstack/react-query";

import HistoryDrawer from "./history-drawer";
import { recordHistoryInfiniteQueryOptions } from "../../query-options";

interface RecordHistoryDrawerProps {
  recordsetId: string;
  recordId: number;
}

const RecordHistoryDrawer = ({
  recordsetId,
  recordId,
}: RecordHistoryDrawerProps) => {
  const query = useInfiniteQuery(
    recordHistoryInfiniteQueryOptions(recordsetId, recordId),
  );

  return <HistoryDrawer subtitle="Recent changes" query={query} />;
};

export default RecordHistoryDrawer;
