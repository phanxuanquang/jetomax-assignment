# ChatApp Frontend

Realtime chat PWA — sign in with Google, message 1:1 or in groups, send images (AI-captioned), request an AI summary of a conversation. React + Vite + TypeScript SPA that talks to the ASP.NET Core backend in `../backend/` over REST and SignalR.

Product brief this was built from: [`../frontend-docs/`](../frontend-docs/). Architecture as actually built: [`docs/`](docs/).

## Features

- **Sign-in** — Google only, via Supabase Auth. No password form, no separate sign-up.
- **Conversation list** — most-recently-active first, filterable by name.
- **Create / join** — create by username (2 participants = direct chat, more = group), or join an existing one by its 6-character `PublicId` code.
- **Realtime messaging** — text and images, delivered live over SignalR to every online participant; history loads via paginated REST and a reconnect never loses or duplicates messages.
- **Image messages** — uploaded straight from the browser to Supabase Storage (backend never sees the bytes); an AI caption attaches asynchronously and updates the message once ready.
- **Conversation summary** — on-demand AI summary of everything sent so far.
- **Owner management** — rename, add/remove participants (batch, all-or-nothing), transfer ownership, toggle read-only, leave with an explicit delete-or-freeze choice (owner) or a plain leave (everyone else).
- **Installable PWA** — manifest + service worker (app-shell precache), installable on `https://` or `localhost` origins.

Explicitly not built (out of scope per the product brief): push notifications, end-to-end encryption, in-app message search, voice/video, multi-owner conversations, any role-aware UI.

## Tech stack

| Concern | Library |
|---|---|
| App / build | React 19, Vite 8, TypeScript |
| UI components | shadcn/ui (Nova preset, Radix primitives) + Tailwind CSS v4 |
| Icons | lucide-react |
| Server state / caching | TanStack Query |
| REST client | Axios |
| Auth + Storage | `@supabase/supabase-js` (Google OAuth, image uploads) |
| Realtime | `@microsoft/signalr` |
| Routing | react-router-dom |
| PWA | vite-plugin-pwa |
| Linting | oxlint |

## Prerequisites

- Node.js 20+
- A Supabase project with Google OAuth enabled and a private Storage bucket named `images`
- The backend (`../backend/`) running and reachable — see [`../backend/README.md`](../backend/README.md)

## Setup

```bash
cd frontend
npm install
cp .env.example .env.local   # then fill in real values, see table below
npm run dev
```

App runs at `http://localhost:5173`.

### Environment variables (`frontend/.env.local`, never committed)

| Variable | Meaning |
|---|---|
| `VITE_SUPABASE_URL` | Your Supabase project URL, e.g. `https://xxxx.supabase.co` |
| `VITE_SUPABASE_ANON_KEY` | Supabase anon/public key — safe to embed client-side, but still project-specific |
| `VITE_API_BASE_URL` | Backend base URL, e.g. `http://localhost:5000` for local dev |

All three are read at **build time** by Vite (`import.meta.env.*`), not at runtime — a production build bakes in whatever was set when `npm run build` ran.

### Running both backend and frontend together

From the repo root: `./start-dev.ps1` (see [`../start-dev.ps1`](../start-dev.ps1)) launches the backend (`dotnet run`) and this app (`npm run dev`) each in their own window.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Start the Vite dev server |
| `npm run build` | Type-check (`tsc -b`) then production build to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm run lint` | Run oxlint |

## Project structure

```
frontend/
├── src/
│   ├── app/            Composition root — providers.tsx, routes.tsx
│   ├── features/       One folder per feature: auth, conversations (+ manage/),
│   │                    messages, realtime, summary, users
│   ├── components/     ui/ = shadcn primitives (generated); shared components elsewhere
│   ├── lib/             api/, supabase/, signalr/, query/ — the non-React infrastructure layer
│   ├── types/           TS interfaces mirroring the backend's data model
│   └── hooks/           Small cross-feature hooks
├── public/              Static assets — manifest icons, favicon
├── docs/                 architecture.md, decisions.md — the real build, not a plan
├── vite.config.ts        Tailwind + PWA plugin config, dev server binds 0.0.0.0
└── .env.local            Not committed — see Environment variables above
```

Full breakdown of each folder, state management approach, and how auth/realtime/images actually work: [`docs/architecture.md`](docs/architecture.md). The reasoning behind non-obvious choices (state library, PWA strategy, image URL handling, etc.): [`docs/decisions.md`](docs/decisions.md).

## LAN access (testing from another device)

`./start-dev.ps1` (see root) handles this automatically: it detects this machine's LAN IPv4, adds it to the backend's allowed CORS origins (via a user-secret, not by editing backend config files), and binds the backend to every network interface instead of just `localhost`. The frontend itself resolves its API/SignalR base URL from whatever hostname the page was loaded through (`src/lib/apiBaseUrl.ts`), so a LAN device automatically calls the right host — no need to edit `VITE_API_BASE_URL` for this.

Only one caveat left: **service workers only register on `https://` or `localhost` origins.** A device hitting the app via its LAN IP gets a fully working page, just not the installable/offline PWA shell — only whoever hits `localhost` gets that.

If you're running the two dev servers some other way (not the script), you'll need to replicate this yourself: bind Kestrel to `0.0.0.0` (e.g. `dotnet run -- --urls http://0.0.0.0:5000`) and add your LAN origin to the backend's `CORS:AllowedOrigins`.

## Deploying

`npm run build` produces a static `dist/` folder — deploy it to any static host (Vercel, Netlify, Nginx, etc.) that:
- serves `index.html` for unknown paths (SPA fallback — this is a client-routed app), and
- terminates HTTPS (required for the PWA/service worker to register).

Set the three `VITE_*` env vars in that host's build environment before building.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Blank page, console error about `supabaseUrl` | `.env.local` missing or empty — see Setup above |
| Sign-in redirects, then nothing happens | Google OAuth not enabled/configured on the Supabase project, or its redirect URL doesn't include this app's origin |
| API calls fail with a CORS error in the console | Backend's `CORS:AllowedOrigins` doesn't include the origin you're loading the frontend from |
| `401` on every API call after signing in | Backend's Supabase JWT validation config (issuer/audience) doesn't match this frontend's Supabase project |
| Images never load, console shows a storage error | The `images` bucket doesn't exist yet, or isn't named exactly `images` |
| "Not connected" error when sending a message | SignalR hasn't finished connecting yet, or the backend isn't reachable at `VITE_API_BASE_URL` |
