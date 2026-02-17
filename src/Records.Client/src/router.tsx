import * as React from "react";

import { QueryClient } from "@tanstack/react-query";
import { createBrowserRouter, Navigate, redirect } from "react-router-dom";

import {
  CreateRecordPage,
  CreateRecordsetPage,
  EditRecordPage,
  EditRecordsetPage,
  ForgotPasswordPage,
  getRecordsetSearch,
  RecordDetailsPage,
  RecordsPage,
  RecordsetsPage,
  ResetPasswordPage,
  SignInPage,
  SignUpPage,
  TenantsAdminPage,
  UsersAdminPage,
} from "./pages";
import {
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
  tenantSummariesQueryOptions,
  notificationRulesQueryOptions,
  pagedRecordsQueryOptions,
  userSummariesQueryOptions,
} from "./query-options";
import Shell from "./shell";

interface IdentityAccessResponse {
  isGlobalAdmin?: boolean;
}

const buildSignInRedirect = (request: Request) => {
  const url = new URL(request.url);
  const callbackUrl = `${url.pathname}${url.search}${url.hash}`;
  const search = callbackUrl
    ? `?callbackUrl=${encodeURIComponent(callbackUrl)}`
    : "";
  return redirect(`/sign-in${search}`);
};

const ensureGlobalAdminAccess = async (request: Request) => {
  const response = await fetch("/identity/access", {
    method: "GET",
    credentials: "include",
  });

  if (response.status === 401) {
    throw buildSignInRedirect(request);
  }

  if (!response.ok) {
    throw redirect("/");
  }

  const access = (await response.json()) as IdentityAccessResponse;
  if (access.isGlobalAdmin !== true) {
    throw redirect("/");
  }
};

export const createAppRouter = (queryClient: QueryClient) =>
  createBrowserRouter([
    {
      path: "/",
      element: <Shell />,
      children: [
        {
          index: true,
          element: <RecordsetsPage />,
        },
        {
          path: "create",
          element: <CreateRecordsetPage />,
        },
        {
          path: ":recordsetId",
          loader: async ({ params }) => {
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
              index: true,
              loader: async ({ request, params }) => {
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
              path: "edit",
              loader: async ({ params }) => {
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
            {
              path: "create",
              element: <CreateRecordPage />,
            },
            {
              path: ":recordId",
              loader: async ({ params }) => {
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
                  loader: async ({ params }) => {
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
          path: "admin/tenants",
          loader: async ({ request }) => {
            await ensureGlobalAdminAccess(request);
            await queryClient.ensureQueryData(tenantSummariesQueryOptions());
            return null;
          },
          element: <TenantsAdminPage />,
        },
        {
          path: "admin/users",
          loader: async ({ request }) => {
            await ensureGlobalAdminAccess(request);
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
      path: "/sign-in",
      element: <SignInPage />,
    },
    {
      path: "/sign-up",
      element: <SignUpPage />,
    },
    {
      path: "/forgot-password",
      element: <ForgotPasswordPage />,
    },
    {
      path: "/reset-password",
      element: <ResetPasswordPage />,
    },
    {
      path: "*",
      element: <Navigate to="/" replace />,
    },
  ]);
