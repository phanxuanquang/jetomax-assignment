import { Navigate, Route, Routes } from "react-router-dom";
import { Loader2 } from "lucide-react";
import { useSession } from "@/features/auth/SessionContext";
import { SignInScreen } from "@/features/auth/SignInScreen";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { ChatLayout } from "@/features/conversations/ChatLayout";
import { ConversationEmptyState } from "@/features/conversations/ConversationEmptyState";
import { ConversationScreen } from "@/features/conversations/ConversationScreen";

function SignInRoute() {
  const { session, isLoading } = useSession();

  if (isLoading) {
    return (
      <main className="flex min-h-svh items-center justify-center">
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </main>
    );
  }

  if (session) {
    return <Navigate to="/" replace />;
  }

  return <SignInScreen />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/sign-in" element={<SignInRoute />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<ChatLayout />}>
          <Route path="/" element={<ConversationEmptyState />} />
          <Route path="/conversations/:conversationId" element={<ConversationScreen />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
