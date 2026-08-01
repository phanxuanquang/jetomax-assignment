import { Navigate, Route, Routes, useSearchParams } from "react-router-dom";
import { Loader2 } from "lucide-react";
import { useSession } from "@/features/auth/SessionContext";
import { SignInScreen } from "@/features/auth/SignInScreen";
import { ProtectedRoute } from "@/features/auth/ProtectedRoute";
import { OAuthConsentScreen } from "@/features/auth/OAuthConsentScreen";
import { ChatLayout } from "@/features/conversations/ChatLayout";
import { ConversationEmptyState } from "@/features/conversations/ConversationEmptyState";
import { ConversationScreen } from "@/features/conversations/ConversationScreen";

function SignInRoute() {
  const { session, isLoading } = useSession();
  const [searchParams] = useSearchParams();
  const redirect = searchParams.get("redirect") ?? "/";

  if (isLoading) {
    return (
      <main className="flex min-h-svh items-center justify-center">
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </main>
    );
  }

  if (session) {
    return <Navigate to={redirect} replace />;
  }

  return <SignInScreen redirect={redirect} />;
}

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/sign-in" element={<SignInRoute />} />
      <Route path="/oauth/consent" element={<OAuthConsentScreen />} />
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
