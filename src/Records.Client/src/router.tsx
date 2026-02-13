import * as React from "react";

import { QueryClient } from "@tanstack/react-query";
import { createBrowserRouter, Navigate } from "react-router-dom";

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
} from "./pages";
import {
  recordQueryOptions,
  recordsetItemDefinitionQueryOptions,
  notificationRulesQueryOptions,
  pagedRecordsQueryOptions,
} from "./query-options";
import Shell from "./shell";

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
