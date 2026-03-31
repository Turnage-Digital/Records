import * as React from "react";
import { Suspense } from "react";

import {
  CssBaseline,
  StyledEngineProvider,
  ThemeProvider,
} from "@mui/material";
import { AdapterDateFns } from "@mui/x-date-pickers/AdapterDateFns";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router-dom";

import { AuthProvider } from "./auth";
import DetailPanel from "./components/detail-panel/detail-panel";
import DetailPanelProvider from "./components/detail-panel/detail-panel-provider";
import Loading from "./components/loading";
import { createAppRouter } from "./router";
import theme from "./theme";

export const queryClient = new QueryClient();
export const router = createAppRouter(queryClient);

const rootElement = document.getElementById("root");
const root = createRoot(rootElement!);

root.render(
  <StyledEngineProvider injectFirst>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <LocalizationProvider dateAdapter={AdapterDateFns}>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <DetailPanelProvider>
              <DetailPanel />
              <Suspense fallback={<Loading />}>
                <RouterProvider router={router} />
              </Suspense>
              {/* <ReactQueryDevtools />*/}
            </DetailPanelProvider>
          </AuthProvider>
        </QueryClientProvider>
      </LocalizationProvider>
    </ThemeProvider>
  </StyledEngineProvider>,
);
