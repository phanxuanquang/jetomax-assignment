import { Outlet, useMatch } from "react-router-dom";
import { ConversationSidebar } from "./ConversationSidebar";

/**
 * Two-pane desktop shell: sidebar + active conversation, both visible at once
 * on md+ screens. On narrow screens only one pane shows at a time (sidebar at
 * "/", the conversation at "/conversations/:id"), same as a typical mobile
 * chat app — this ternary is what switches between the two behaviors.
 */
export function ChatLayout() {
  const isDetailRoute = Boolean(useMatch("/conversations/:conversationId"));

  return (
    <div className="flex h-svh overflow-hidden">
      <ConversationSidebar
        className={isDetailRoute ? "hidden md:flex md:w-80 md:shrink-0" : "flex w-full md:w-80 md:shrink-0"}
      />
      <main className={`min-w-0 flex-1 flex-col ${isDetailRoute ? "flex" : "hidden md:flex"}`}>
        <Outlet />
      </main>
    </div>
  );
}
