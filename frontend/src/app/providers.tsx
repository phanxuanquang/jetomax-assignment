import type { ReactNode } from "react";
import { BrowserRouter } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { queryClient } from "@/lib/query/client";
import { SessionProvider } from "@/features/auth/SessionContext";
import { HubProvider } from "@/features/realtime/HubProvider";
import { TooltipProvider } from "@/components/ui/tooltip";
import { Toaster } from "@/components/ui/sonner";

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <SessionProvider>
            <HubProvider>
              <TooltipProvider>
                {children}
                <Toaster position="top-center" richColors />
              </TooltipProvider>
            </HubProvider>
          </SessionProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
