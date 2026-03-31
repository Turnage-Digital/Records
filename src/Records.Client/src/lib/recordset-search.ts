import type { RecordsetSearch } from "../models/recordset-search";

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
    status,
    field,
    sort,
  };
};

export const setRecordsetSearchParams = (
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
