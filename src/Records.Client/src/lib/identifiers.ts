const ULID_REGEX = /^[0-9A-HJKMNP-TV-Z]{26}$/i;
const FALLBACK_ACTOR_ULID = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

const readString = (value: unknown): string | undefined => {
  return typeof value === "string" && value.length > 0 ? value : undefined;
};

const readNumber = (value: unknown): number | undefined => {
  return typeof value === "number" && Number.isFinite(value)
    ? value
    : undefined;
};

export const isUlid = (value: string): boolean => ULID_REGEX.test(value);

export const resolveActorUlid = (
  user: Record<string, unknown> | null | undefined,
): string => {
  if (user) {
    const candidateKeys = [
      "id",
      "userId",
      "sub",
      "nameIdentifier",
      "nameid",
    ] as const;

    for (const key of candidateKeys) {
      const candidate = readString(user[key]);
      if (candidate && isUlid(candidate)) {
        return candidate;
      }
    }
  }

  return FALLBACK_ACTOR_ULID;
};

export const resolveTenantUlid = (
  user: Record<string, unknown> | null | undefined,
): string | undefined => {
  if (!user) {
    return undefined;
  }

  const candidateKeys = [
    "tenantId",
    "tenantid",
    "tenant_id",
    "tid",
    "tenant",
  ] as const;

  for (const key of candidateKeys) {
    const candidate = readString(user[key]);
    if (candidate && isUlid(candidate)) {
      return candidate;
    }
  }

  return undefined;
};

export const extractRecordsetId = (payload: unknown): string | undefined => {
  if (!payload || typeof payload !== "object") {
    return undefined;
  }

  const mapped = payload as Record<string, unknown>;
  const id = readString(mapped.id);
  if (id) {
    return id;
  }

  return readString(mapped.recordsetId);
};

export const extractRecordId = (payload: unknown): number | undefined => {
  if (!payload || typeof payload !== "object") {
    return undefined;
  }

  const mapped = payload as Record<string, unknown>;
  const id = readNumber(mapped.id);
  if (typeof id === "number") {
    return id;
  }

  const recordId = readNumber(mapped.recordId);
  if (typeof recordId === "number") {
    return recordId;
  }

  const stringId = readString(mapped.id) ?? readString(mapped.recordId);
  if (!stringId) {
    return undefined;
  }

  const parsed = Number(stringId);
  return Number.isFinite(parsed) ? parsed : undefined;
};
