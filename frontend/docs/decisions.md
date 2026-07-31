# Decisions

Things that aren't obvious just from reading the code, and why they ended up this way. Background/validation for each of these lives in the conversation this app was built from; this is the durable record.

## State management: TanStack Query + Context, no Redux/Zustand

Server state (conversations, messages, summary) needs caching, pagination, and invalidation — that's what TanStack Query is for, and realtime patches slot directly into its cache via `setQueryData`/`invalidateQueries`. The only client-only state left (current session, SignalR connection) is small enough that a second state library would be pure ceremony.

## PWA offline strategy: app-shell precache only

Considered: full offline-first with a send queue + background sync. Rejected — queuing messages sent while offline and replaying them on reconnect risks duplicate or out-of-order sends in a realtime chat, which is a worse failure mode than "you need network to send a message." App-shell precache (install, boot offline, static assets cached) covers what a PWA needs to *be* installable without taking on that risk.

## No backend endpoint for userId → username, until there was

`ConversationDto`/`MessageDto` only return user **ids**; the API takes literal usernames on the way in (create/add/transfer) but never returns them. This was flagged as an open gap during planning — the fallback plan was a best-effort local cache of usernames the frontend had typed in itself, plus a masked-id display (`User 8f3a21`) for anyone else. Mid-build, `GET /api/users/{idOrUsername}` was added to the backend specifically to close this gap, so the fallback was dropped in favor of resolving every id for real, cached per id (see `features/users/useUsername.ts`).

## Image URL: store the storage path, not a signed URL

The `images` Storage bucket is private, so a public URL 403s for every viewer but the uploader. Two options were on the table: bake a long-lived signed URL into the message at send time (simple, but expires and breaks that image forever once it does), or store the bare `images/<path>` and have each viewer resolve a fresh signed URL at render time. Went with the latter — confirmed the backend's own storage code already parses this exact bare-path shape for its own purposes (AI captioning re-downloads by path), so it's not a client-side invention.

## Dev target: localhost + LAN, no deploy config yet

Current need is local-only, with other devices on the LAN reaching the frontend by IP. `vite.config.ts` sets `server.host: true` for that. Not addressed yet: a LAN device hitting the app via a plain-`http://` IP address gets a normal web page, not an installable PWA — service workers require `https://` or `localhost`. Also not addressed: the backend's CORS allowlist only has `http://localhost:5173` by default, so a LAN-IP origin needs to be added there too before cross-origin API calls from another device will succeed. Both are called out in the root README rather than silently worked around, since fixing them (a dev HTTPS cert, or a CORS entry) is an environment decision, not a frontend code change.

## Known follow-ups, not addressed

- **Bundle size**: the production build is a single ~740 KB JS chunk (see the build's own warning). Route-level code-splitting (`React.lazy` per route) would help; not done because the app has exactly two routes today and the added complexity wasn't worth it yet.
- **PWA icons are placeholders**: solid-color squares matching the theme color, generated to get a valid manifest in place. Swap `public/pwa-192x192.png` / `public/pwa-512x512.png` for real branded icons before shipping.
