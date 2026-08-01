import { useState } from "react";
import { MessageCircle } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { supabase } from "@/lib/supabase/client";
import { GoogleIcon } from "./GoogleIcon";

export function SignInScreen({ redirect = "/" }: { redirect?: string }) {
  const [isSigningIn, setIsSigningIn] = useState(false);

  async function handleSignIn() {
    setIsSigningIn(true);
    const { error } = await supabase.auth.signInWithOAuth({
      provider: "google",
      options: {
        // Forces Google's account chooser every time, instead of silently
        // re-using whichever Google account is currently logged into the browser.
        queryParams: { prompt: "select_account" },
        redirectTo: `${window.location.origin}${redirect}`,
      },
    });
    if (error) {
      toast.error(error.message);
      setIsSigningIn(false);
    }
    // On success the browser redirects away for the OAuth round-trip, so no
    // further local state update is needed here.
  }

  return (
    <main className="flex min-h-svh flex-col items-center justify-center gap-8 p-6">
      <div className="flex flex-col items-center gap-2 text-center">
        <MessageCircle className="size-10 text-primary" aria-hidden="true" />
        <h1 className="text-2xl font-semibold">ChatApp</h1>
        <p className="text-muted-foreground text-sm">Sign in to start messaging.</p>
      </div>
      <Button
        onClick={handleSignIn}
        disabled={isSigningIn}
        size="lg"
        variant="outline"
        className="gap-3"
      >
        <GoogleIcon className="size-5" />
        {isSigningIn ? "Redirecting to Google..." : "Continue with Google"}
      </Button>
    </main>
  );
}
