import * as React from "react";

import { QueryClient } from "@tanstack/react-query";
import { createBrowserRouter, Navigate, redirect } from "react-router-dom";

import { getRecordsetSearch } from "./lib/recordset-search";
import {
  agentThreadQueryOptions,
  agentThreadSummariesQueryOptions,
  notificationRulesQueryOptions,
  pagedRecordsQueryOptions,
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
  sessionQueryOptions,
  tenantSummariesQueryOptions,
  userSummariesQueryOptions,
} from "./query-options";

const AccessDeniedPage = React.lazy(() => import("./pages/access-denied-page"));
const AgentsHomePage = React.lazy(() => import("./pages/agents-home-page"));
const AgentThreadPage = React.lazy(() => import("./pages/agent-thread-page"));
const Shell = React.lazy(() => import("./shell"));
const CreateRecordPage = React.lazy(() => import("./pages/create-record-page"));
const CreateRecordsetPage = React.lazy(
  () => import("./pages/create-recordset-page"),
);
const EditRecordPage = React.lazy(() => import("./pages/edit-record-page"));
const EditRecordsetPage = React.lazy(
  () => import("./pages/edit-recordset-page"),
);
const ForgotPasswordPage = React.lazy(
  () => import("./pages/forgot-password-page"),
);
const RecordDetailsPage = React.lazy(
  () => import("./pages/record-details-page"),
);
const RecordsetsPage = React.lazy(() => import("./pages/recordsets-page"));
const RecordsPage = React.lazy(() => import("./pages/records-page"));
const ResetPasswordPage = React.lazy(
  () => import("./pages/reset-password-page"),
);
const SignInPage = React.lazy(() => import("./pages/sign-in-page"));
const SignUpPage = React.lazy(() => import("./pages/sign-up-page"));
const TenantsAdminPage = React.lazy(() => import("./pages/tenants-admin-page"));
const UsersAdminPage = React.lazy(() => import("./pages/users-admin-page"));

const buildSignInRedirect = (request: Request) => {
  const url = new URL(request.url);
  const callbackUrl = `${url.pathname}${url.search}${url.hash}`;
  const search = callbackUrl
    ? `?callbackUrl=${encodeURIComponent(callbackUrl)}`
    : "";
  return redirect(`/sign-in${search}`);
};

const getPostAuthDestination = (request: Request) =>
  new URL(request.url).searchParams.get("callbackUrl") ?? "/";

const loadSession = (queryClient: QueryClient) =>
  queryClient.ensureQueryData(sessionQueryOptions());

const requireOpsSession = async (
  queryClient: QueryClient,
  request: Request,
) => {
  const session = await loadSession(queryClient);

  if (!session) {
    throw buildSignInRedirect(request);
  }

  if (!session.access.canAccessOps) {
    throw redirect("/access-denied");
  }

  return session;
};

const requireGlobalAdminSession = async (
  queryClient: QueryClient,
  request: Request,
) => {
  const session = await requireOpsSession(queryClient, request);
  if (!session.access.isGlobalAdmin) {
    throw redirect("/");
  }

  return session;
};

const redirectAuthenticatedUser = async (
  queryClient: QueryClient,
  request: Request,
) => {
  const session = await loadSession(queryClient);

  if (!session) {
    return null;
  }

  throw redirect(
    session.access.canAccessOps
      ? getPostAuthDestination(request)
      : "/access-denied",
  );
};

const ensureAgentThreadData = async (
  queryClient: QueryClient,
  threadId?: string,
) => {
  await queryClient.ensureQueryData(agentThreadSummariesQueryOptions());

  if (threadId) {
    await queryClient.ensureQueryData(agentThreadQueryOptions(threadId));
  }
};

export const createAppRouter = (queryClient: QueryClient) =>
  createBrowserRouter([
    {
      path: "/",
      loader: async ({ request }) => {
        await requireOpsSession(queryClient, request);
        return null;
      },
      element: <Shell />,
      children: [
        {
          index: true,
          loader: async ({ request }) => {
            await requireOpsSession(queryClient, request);
            await ensureAgentThreadData(queryClient);
            return null;
          },
          element: <AgentsHomePage />,
        },
        {
          path: "threads/:threadId",
          loader: async ({ params, request }) => {
            const threadId = params.threadId;
            if (!threadId) {
              throw new Response("Not Found", { status: 404 });
            }

            await requireOpsSession(queryClient, request);
            await ensureAgentThreadData(queryClient, threadId);
            return null;
          },
          element: <AgentThreadPage />,
        },
        {
          path: "recordsets",
          children: [
            {
              index: true,
              loader: async ({ request }) => {
                await requireOpsSession(queryClient, request);
                return null;
              },
              element: <RecordsetsPage />,
            },
            {
              path: "create",
              loader: async ({ request }) => {
                await requireOpsSession(queryClient, request);
                return null;
              },
              element: <CreateRecordsetPage />,
            },
            {
              path: ":recordsetId",
              loader: async ({ params, request }) => {
                await requireOpsSession(queryClient, request);
                const recordsetId = params.recordsetId;
                if (!recordsetId) {
                  throw new Response("Not Found", { status: 404 });
                }
                await queryClient.ensureQueryData(
                  recordsetItemDefinitionQueryOptions(recordsetId),
                );
                return null;
              },
              children: [
                {
                  path: "records",
                  children: [
                    {
                      index: true,
                      loader: async ({ request, params }) => {
                        await requireOpsSession(queryClient, request);
                        const recordsetId = params.recordsetId;
                        if (!recordsetId) {
                          throw new Response("Not Found", { status: 404 });
                        }
                        const searchParams = new URL(request.url).searchParams;
                        const search = getRecordsetSearch(searchParams);
                        await queryClient.ensureQueryData(
                          pagedRecordsQueryOptions(search, recordsetId),
                        );
                        return null;
                      },
                      element: <RecordsPage />,
                    },
                    {
                      path: "create",
                      loader: async ({ request }) => {
                        await requireOpsSession(queryClient, request);
                        return null;
                      },
                      element: <CreateRecordPage />,
                    },
                    {
                      path: ":recordId",
                      loader: async ({ params, request }) => {
                        await requireOpsSession(queryClient, request);
                        const recordsetId = params.recordsetId;
                        const recordId = params.recordId;
                        if (!recordsetId || !recordId) {
                          throw new Response("Not Found", { status: 404 });
                        }
                        await queryClient.ensureQueryData(
                          recordQueryOptions(recordsetId, Number(recordId)),
                        );
                        return null;
                      },
                      children: [
                        {
                          index: true,
                          element: <RecordDetailsPage />,
                        },
                        {
                          path: "edit",
                          loader: async ({ params, request }) => {
                            await requireOpsSession(queryClient, request);
                            const recordsetId = params.recordsetId;
                            const recordId = params.recordId;
                            if (!recordsetId || !recordId) {
                              throw new Response("Not Found", { status: 404 });
                            }
                            await queryClient.ensureQueryData(
                              recordQueryOptions(recordsetId, Number(recordId)),
                            );
                            return null;
                          },
                          element: <EditRecordPage />,
                        },
                      ],
                    },
                  ],
                },
                {
                  path: "edit",
                  loader: async ({ params, request }) => {
                    await requireOpsSession(queryClient, request);
                    const recordsetId = params.recordsetId;
                    if (!recordsetId) {
                      throw new Response("Not Found", { status: 404 });
                    }
                    await queryClient.ensureQueryData(
                      notificationRulesQueryOptions(recordsetId),
                    );
                    return null;
                  },
                  element: <EditRecordsetPage />,
                },
              ],
            },
          ],
        },
        {
          path: "admin/tenants",
          loader: async ({ request }) => {
            await requireGlobalAdminSession(queryClient, request);
            await queryClient.ensureQueryData(tenantSummariesQueryOptions());
            return null;
          },
          element: <TenantsAdminPage />,
        },
        {
          path: "admin/users",
          loader: async ({ request }) => {
            await requireGlobalAdminSession(queryClient, request);
            await Promise.all([
              queryClient.ensureQueryData(userSummariesQueryOptions()),
              queryClient.ensureQueryData(tenantSummariesQueryOptions()),
            ]);
            return null;
          },
          element: <UsersAdminPage />,
        },
      ],
    },
    {
      path: "/access-denied",
      loader: async ({ request }) => {
        const session = await loadSession(queryClient);

        if (!session) {
          throw buildSignInRedirect(request);
        }

        if (session.access.canAccessOps) {
          throw redirect("/");
        }

        return null;
      },
      element: <AccessDeniedPage />,
    },
    {
      path: "/sign-in",
      loader: async ({ request }) => {
        await redirectAuthenticatedUser(queryClient, request);
        return null;
      },
      element: <SignInPage />,
    },
    {
      path: "/sign-up",
      loader: async ({ request }) => {
        await redirectAuthenticatedUser(queryClient, request);
        return null;
      },
      element: <SignUpPage />,
    },
    {
      path: "/forgot-password",
      loader: async ({ request }) => {
        await redirectAuthenticatedUser(queryClient, request);
        return null;
      },
      element: <ForgotPasswordPage />,
    },
    {
      path: "/reset-password",
      loader: async ({ request }) => {
        await redirectAuthenticatedUser(queryClient, request);
        return null;
      },
      element: <ResetPasswordPage />,
    },
    {
      path: "*",
      element: <Navigate to="/" replace />,
    },
  ]);
