import { useEffect, useState, type ReactNode } from "react";
import { Navigate, useSearchParams } from "react-router-dom";
import { Loader2 } from "lucide-react";
import type { OAuthAuthorizationDetails } from "@supabase/supabase-js";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { supabase } from "@/lib/supabase/client";
import { useSession } from "./SessionContext";

function CenteredNote({ children }: { children: ReactNode }) {
  return <main className="flex min-h-svh items-center justify-center p-6">{children}</main>;
}

// Renders the consent screen Supabase's OAuth 2.1 server redirects to mid-authorization-code-flow
// (Authentication -> OAuth Server -> Authorization Path). ChatGPT/Claude never call this route
// directly — it's the human's own browser that lands here.
export function OAuthConsentScreen() {
  const { session, isLoading: isSessionLoading } = useSession();
  const [searchParams] = useSearchParams();
  const authorizationId = searchParams.get("authorization_id");

  const [details, setDetails] = useState<OAuthAuthorizationDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (isSessionLoading || !session || !authorizationId) {
      return;
    }

    supabase.auth.oauth.getAuthorizationDetails(authorizationId).then(({ data, error }) => {
      if (error) {
        setError(error.message);
      } else if ("authorization_id" in data) {
        setDetails(data);
      } else {
        // Already consented for these scopes — no UI needed, go straight back to the client.
        window.location.href = data.redirect_url;
      }
    });
  }, [isSessionLoading, session, authorizationId]);

  async function respond(decision: "approve" | "deny") {
    if (!authorizationId) {
      return;
    }
    setIsSubmitting(true);
    const { data, error } =
      decision === "approve"
        ? await supabase.auth.oauth.approveAuthorization(authorizationId, { skipBrowserRedirect: true })
        : await supabase.auth.oauth.denyAuthorization(authorizationId, { skipBrowserRedirect: true });

    if (error) {
      setError(error.message);
      setIsSubmitting(false);
      return;
    }
    window.location.href = data.redirect_url;
  }

  if (!authorizationId) {
    return <CenteredNote>Missing authorization request.</CenteredNote>;
  }

  if (isSessionLoading) {
    return (
      <CenteredNote>
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </CenteredNote>
    );
  }

  if (!session) {
    const redirect = encodeURIComponent(`/oauth/consent?authorization_id=${authorizationId}`);
    return <Navigate to={`/sign-in?redirect=${redirect}`} replace />;
  }

  if (error) {
    return <CenteredNote>Error: {error}</CenteredNote>;
  }

  if (!details) {
    return (
      <CenteredNote>
        <Loader2 className="size-6 animate-spin text-muted-foreground" aria-hidden="true" />
      </CenteredNote>
    );
  }

  const scopes = details.scope.trim() ? details.scope.split(" ") : [];

  return (
    <CenteredNote>
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Authorize {details.client.name}</CardTitle>
          <CardDescription>
            This app wants to access your ChatApp account ({details.user.email}).
          </CardDescription>
        </CardHeader>
        {scopes.length > 0 && (
          <CardContent className="space-y-2 text-sm">
            <p className="font-medium">Requested permissions</p>
            <ul className="list-disc pl-5 text-muted-foreground">
              {scopes.map((scope) => (
                <li key={scope}>{scope}</li>
              ))}
            </ul>
          </CardContent>
        )}
        <CardFooter className="justify-end gap-2">
          <Button variant="outline" disabled={isSubmitting} onClick={() => respond("deny")}>
            Deny
          </Button>
          <Button disabled={isSubmitting} onClick={() => respond("approve")}>
            Allow
          </Button>
        </CardFooter>
      </Card>
    </CenteredNote>
  );
}
