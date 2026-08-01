# Architecture

## Folder structure

```
src/
  app/              Composition root: providers.tsx (context/provider nesting),
                    routes.tsx (route table). Nothing feature-specific lives here.
  features/
    auth/           Sign-in screen, session context (Supabase auth state), protected-route guard
    conversations/  List, filter, create, join, the conversation screen shell,
                    and manage/ (owner-only rename/participants/transfer/readonly/leave)
    messages/       Message list (paginated + realtime), composer, image send/render
    realtime/       SignalR connection lifecycle + the functions that patch the
                    TanStack Query cache when a broadcast arrives
    summary/        The one-off AI summary request/response dialog
    users/          userId -> username resolution (hook + cache + display component)
  components/
    ui/             shadcn/ui primitives — generated, don't hand-edit
    UsernameChipInput.tsx  Shared multi-username entry control (create / add-participants)
  lib/
    api/            Axios instance (JWT interceptor, error unwrapping) + one file per
                    REST resource (conversations.ts, users.ts) — thin wrappers, no logic
    supabase/       Supabase client, and the image-upload / signed-URL storage helpers
    signalr/        Builds a HubConnection (accessTokenFactory, auto-reconnect) — no
                    React here, features/realtime/HubProvider.tsx is the React half
    query/          TanStack Query client + the single source of truth for query keys
    format.ts       Small date/time formatting helpers
  types/            Interfaces mirroring ../../backend/docs/database-design.md, 1:1 with the API
  hooks/            Cross-feature hooks with no feature of their own (useDebouncedValue)
```

**Why feature-first at the top, layer-first inside each feature:** everything about "managing a conversation" lives under `features/conversations/manage/` — a change to that flow touches one folder, not four scattered by type. Cross-cutting concerns (the Axios client, the query client, the SignalR connection) get their own `lib/` layer because they're infrastructure, not a feature.

## State management

**TanStack Query owns all server state** — conversations, paginated messages, the summary request. There is no separate client-side store duplicating what the API already knows; the query cache *is* the state.

**Realtime patches the same cache** instead of living in a parallel store. `features/realtime/HubProvider.tsx` opens one SignalR connection per signed-in session and, on each broadcast, calls into `features/realtime/cachePatchers.ts`:

- `NewMessage` → upserts the message into that conversation's cached page (by id — this is also how a caption that arrives after the image gets applied, since it's the same message id arriving again with `caption` now set) and bumps that conversation's `lastMessageTime` in every cached list.
- `MemberChanged` → if it's *you* being removed/leaving, the conversation is dropped from the cached list immediately (no round-trip); any other membership change just invalidates the list, since those are rare and a refetch is cheap.
- `onreconnected` → invalidates every conversation-related query, so a connection drop-and-recover self-heals from whatever REST would show right now rather than trusting whatever was missed on the wire.

One consequence worth knowing: **there's no `GET /api/conversations/{id}` endpoint.** The conversation screen reads the same unfiltered list query (`useConversations("")`) that the list screen uses and finds its conversation by id — it's the same cache entry, kept live by the same realtime patches, not a second fetch path to keep in sync.

**Everything else is plain React Context**, because there isn't enough of it to justify a state library:
- `SessionContext` — the Supabase session/user, subscribed via `onAuthStateChange`.
- `HubContext` — `sendTextMessage`/`sendImageMessage` plus connection status, exposed via `useHub()`.

## Auth flow

1. `SessionProvider` wraps the app, holds the current Supabase `Session`, and re-renders on any `onAuthStateChange` event (sign-in, sign-out, token refresh).
2. The Axios instance (`lib/api/client.ts`) has a request interceptor that calls `supabase.auth.getSession()` fresh on every call and attaches `Authorization: Bearer <token>` — no manually-managed token state to go stale.
3. `ProtectedRoute` (a layout route) redirects to `/sign-in` when there's no session; `/sign-in` itself redirects *away* if a session already exists.
4. `HubProvider` opens a SignalR connection once a session exists, using the identical `getSession()` pattern via `accessTokenFactory`, and tears it down on sign-out.

Supabase's `session.user.id` (the JWT's `sub` claim) is the same GUID the backend uses as `User.Id` everywhere (`ownerId`, `participantUserIds`, `senderUserId`, the `MemberChanged` event's `userId`) — confirmed against the backend's own JWT-validation code, not assumed. Every "is this me?" check in the frontend is a direct string comparison against `session.user.id`, no extra lookup involved.

## Username resolution

The API identifies people by id in every response and by username only in requests. `features/users/useUsername.ts` resolves an id to a username via `GET /api/users/{idOrUsername}`, cached per id with `staleTime: Infinity` — usernames are assigned at sign-up and never editable, so a resolved id is valid for the rest of the session. `<Username userId="..." />` is the drop-in display component used anywhere a person needs to be shown by name (message sender, participant list).

## Image messages

Images never touch the backend. The flow (`features/messages/MessageComposer.tsx` + `lib/supabase/storage.ts`):

1. Upload the file directly to the Supabase Storage `images` bucket (private) at `<conversationId>/<uuid>.<ext>`.
2. Send the **bare `images/<path>` string** — not a URL — as the message's `imageUrl` via the `SendImage` SignalR method.
3. Every viewer resolves that bare path to a short-lived signed URL lazily, at render time (`features/messages/useSignedImageUrl.ts`), with an in-memory cache keyed by path so the same image isn't re-signed on every re-render.

Storing the path instead of a pre-signed URL is deliberate: a signed URL baked into the message forever would eventually expire and break that image for every future viewer, permanently, with no easy fix. The backend's own storage code already parses this exact bare `bucket/path` shape (it does the same thing server-side for AI captioning), so this isn't a guess — it's the shape the backend was already built to expect.

## Message pagination

`features/messages/useMessages.ts` uses `useInfiniteQuery`. Each page comes back newest-first (per the API); the *pages* themselves are also newest-first (page 0 = newest chunk). `flattenMessagePages` reverses both the page order and each page's contents to produce a single oldest-to-newest array for rendering. `MessageList.tsx` uses an `IntersectionObserver` on a sentinel above the oldest loaded message to trigger `fetchNextPage()`, and preserves scroll position across that load (compensating for the new content's height) rather than letting the view jump.

## PWA

`vite-plugin-pwa` with `registerType: 'autoUpdate'` — an app-shell precache only (static assets, install prompt), no offline data/message queue. A realtime chat has a real correctness cost to queuing sends made while offline (duplicate/out-of-order risk); the acceptance bar in [`../../docs/software-requirements-specification.md` §F-3](../../docs/software-requirements-specification.md#f-3--real-time-messaging) cares about a reconnect never losing or duplicating messages, which the realtime + query-invalidation design already covers for online reconnects. Offline compose was scoped out rather than half-built.
