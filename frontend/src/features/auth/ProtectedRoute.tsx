import { Navigate, Outlet } from "react-router-dom";
import { Loader2 } from "lucide-react";
import { useSession } from "./SessionContext";

export function ProtectedRoute() {
  const { session, isLoading } = useSession();

  if (isLoading) {
    return (
      <main className="flex min-h-svh items-center justify-center">
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </main>
    );
  }

  if (!session) {
    return <Navigate to="/sign-in" replace />;
  }

  return <Outlet />;
}
