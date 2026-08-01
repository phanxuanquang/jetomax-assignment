ASP.NET Core (.NET 10) backend for a realtime chat app: REST + SignalR API, Clean Architecture, backed by Supabase (Auth, Storage, Postgres) and Google Gemini for AI features. For the full system design, requirements, and data model, see [docs/](docs/).

## Projects

```
backend/
├── ChatApp.slnx
├── docs/                     architecture, data model, schema — see docs/ above
└── src/
    ├── ChatApp.Domain/          entities, enums — no dependencies
    ├── ChatApp.Application/     MediatR use cases + ports (interfaces)
    ├── ChatApp.Infrastructure/  EF Core, Supabase Storage, Semantic Kernel/Gemini
    └── ChatApp.Api/             host: controllers, SignalR Hub, auth, DI
```

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.x | Build & run the backend |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | latest | Local Supabase stack (skip if using a hosted Supabase project) |
| [Supabase CLI](https://supabase.com/docs/guides/cli) | latest | Run Supabase locally / manage projects |
| A [Google AI Studio](https://aistudio.google.com/apikey) API key | — | Gemini (captioning, summaries) |

```bash
dotnet --version   # 10.x
docker --version
supabase --version
```

Running the frontend too needs [Node.js 20+](https://nodejs.org) — see [`../frontend/README.md`](../frontend/README.md).

## First-time setup

Skip this section if a Supabase project with `docs/schema.sql` already applied exists — jump to [Configure & run](#configure--run).

### 1. Supabase project

**Option A (local, recommended for development)** runs Postgres + Auth + Storage + Studio in Docker:

```bash
supabase init
supabase start
```

`supabase start` prints your local credentials — copy the **API URL**, **anon key**, and **service_role key**.

**Option B (hosted project)**: create one at [supabase.com/dashboard](https://supabase.com/dashboard), then **Project Settings → API** for the same three values.

Either way, you do not need to copy a JWT secret — see the note on JWT validation below.

### 2. Storage bucket

Create a private bucket named `images` (Studio → Storage → New bucket). The frontend uploads images directly and generates its own signed URLs; the backend's only storage need is downloading an object's bytes (with the service-role key) so it can hand them to Gemini for captioning.

`storage.objects` has RLS enabled by default with no policy — an upload fails with "new row violates row-level security policy" until one exists. `docs/schema.sql` (next step) creates the two policies this bucket needs (`storage_images_insert`/`storage_images_select`, any authenticated user); nothing extra to do here as long as you run that script after creating the bucket.

### 3. Google OAuth (Auth — Google only, no email/password)

Set it up in two places:

**Google Cloud Console:**
1. Create/select a project at [console.cloud.google.com](https://console.cloud.google.com).
2. **APIs & Services → OAuth consent screen** → External, fill in app name + support email.
3. **APIs & Services → Credentials → Create Credentials → OAuth Client ID** → Web application.
4. **Authorized JavaScript origins**: your frontend URL (e.g. `http://localhost:5173`).
5. **Authorized redirect URIs**: your Supabase callback — hosted: `https://<project-ref>.supabase.co/auth/v1/callback`; local CLI: `http://127.0.0.1:54321/auth/v1/callback`.
6. Copy the **Client ID** and **Client Secret**.

**Supabase:**
- Hosted: Studio → **Authentication → Providers → Google** → paste Client ID + Secret → Save. Then set **Site URL** and your frontend origin(s) under **Authentication → URL Configuration**.
- Local CLI: add to `supabase/config.toml`:
  ```toml
  [auth.external.google]
  enabled = true
  client_id = "<client id>"
  secret = "env(SUPABASE_AUTH_EXTERNAL_GOOGLE_SECRET)"
  ```
  put the real secret in `supabase/.env`, then `supabase stop && supabase start`.

The frontend calls `supabase.auth.signInWithOAuth({ provider: 'google' })` — no custom sign-up form. Username is derived automatically by the `handle_new_user` trigger in `docs/schema.sql`.

Reference: [Login with Google](https://supabase.com/docs/guides/auth/social-login/auth-google)

> **How the backend validates tokens.** Supabase signs JWTs asymmetrically (ES256) by default on new projects, not with a shared secret you copy around. The backend instead validates against Supabase's **JWKS endpoint** (`{Supabase:Url}/auth/v1/.well-known/jwks.json`), which publishes the current signing key(s) — this needs only `Supabase:Url`, no separate secret. Reference: [JWT Signing Keys](https://supabase.com/docs/guides/auth/signing-keys).

### 4. Apply the database schema

Run `docs/schema.sql` against your Supabase database — it's idempotent, safe to run more than once.

- **Studio:** SQL Editor → paste `docs/schema.sql` → run.
- **CLI:**
  ```bash
  supabase db reset                          # optional: clean slate
  psql "$DATABASE_URL" -f docs/schema.sql    # DATABASE_URL from `supabase status`
  ```

This creates every table, trigger, and RLS policy described in [docs/database-design.md](docs/database-design.md).

## Configure & run

**Fastest path:** copy [`.env.local.example`](.env.local.example) to `.env.local`, fill in the values from step 1 above, then run [`./start-dev.ps1`](start-dev.ps1) — it loads `.env.local` into `dotnet user-secrets` for you and starts the API. Details in [Quick start](#quick-start) below.

**Manual path** — replace the [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) values with the actual values before executing:

```bash
cd backend
dotnet restore
dotnet build

cd src/ChatApp.Api
dotnet user-secrets init
dotnet user-secrets set "SupabaseStorageOptions:ServiceRoleKey" "<service_role key>"
dotnet user-secrets set "ConnectionStrings:Postgres"            "<Postgres connection string>"
dotnet user-secrets set "GeminiConnectionOptions:ApiKey"        "<google ai studio key>"
dotnet user-secrets set "Clients:McpKey"                        "<random secret>"
dotnet user-secrets set "Clients:N8nKey"                        "<random secret>"

dotnet run --project src/ChatApp.Api
```

Full variable list and what each one means: [Environment variables reference](#environment-variables-reference) below.

> **One gotcha.** `ConnectionStrings:Postgres` is a *different* credential from `SupabaseStorageOptions:ServiceRoleKey` — `ServiceRoleKey` is a JWT used as a Bearer token for Supabase's REST APIs, while EF Core needs a real Postgres role and password via Npgsql (**Project Settings → Database → Connection string**, or `supabase status` locally). For a hosted project, use the **"Session Pooler"** connection string, not "Direct connection" — direct connections are IPv6-only on most hosted projects, and Transaction Pooler mode conflicts with Npgsql's default prepared-statement caching under EF Core.
>
> If you're pointing at your own Supabase project rather than the demo one defaulted in `appsettings.json`, also set `Supabase:Url` and `SupabaseStorageOptions:Url` (same value, two config sections — see [Environment variables reference](#environment-variables-reference)).

In `Development`, the API exposes a Scalar API reference (OpenAPI-based) for interactive exploration — check the console output for the exact URL on startup.

## Quick start

Runs [`start-dev.ps1`](start-dev.ps1):

```powershell
cd backend
Copy-Item .env.local.example .env.local   # then fill in the real values
./start-dev.ps1
```

The script: loads `.env.local`, writes every value into `dotnet user-secrets` (safe to re-run — each `set` just overwrites), then runs `dotnet run --project src/ChatApp.Api`. Pass `-Lan` to also bind Kestrel to every network interface and register this machine's LAN IPv4 as an allowed CORS origin, so a phone/another PC on the same network can reach it — see the root [`../start-dev.ps1`](../start-dev.ps1) for running backend + frontend together this way.

## Verify end-to-end

Needs the frontend running too — see [`../frontend/README.md`](../frontend/README.md#setup).

1. Sign in with two different Google accounts.
2. User A creates a conversation, adding User B by username — note the generated `PublicId`.
3. Send text messages both ways — they appear in real time.
4. Send an image — it renders and gets an AI-generated caption.
5. (Optional) User C joins with the `PublicId`.

If all five pass, the environment is wired correctly.

## Environment variables reference

**Backend** (`src/ChatApp.Api`, via user-secrets or environment):

| Key | Meaning |
|---|---|
| `Supabase:Url` | Supabase API URL — used for JWT/JWKS validation. Already defaulted in `appsettings.json` to the demo project; only set to point at your own |
| `SupabaseStorageOptions:Url` | Same Supabase API URL, bound separately for the storage client. Same default/override rule as `Supabase:Url` |
| `SupabaseStorageOptions:ServiceRoleKey` | Service role key — must bypass RLS (see [docs/database-design.md](docs/database-design.md#row-level-security)) |
| `ConnectionStrings:Postgres` | The actual Postgres connection string used by EF Core (not the same credential as `ServiceRoleKey`) |
| `SupabaseStorageOptions:StorageBucket` | `images` — already defaulted in `appsettings.json` |
| `GeminiConnectionOptions:ApiKey` / `GeminiConnectionOptions:ModelId` | Google AI Studio key / model id — Gemini model availability changes over time; if you hit a 404, check [deprecations](https://ai.google.dev/gemini-api/docs/deprecations) and update the config value, no code change needed |
| `Clients:McpKey` / `Clients:N8nKey` | Service keys for the MCP server and n8n — invent any random string, share it with whoever runs those |
| `ConversationMemoryOptions:TokenThreshold` | Tokens accumulated before a memory chunk is formed — already defaulted (1500) |

**Frontend** (`frontend/.env.local`): `VITE_SUPABASE_URL`, `VITE_SUPABASE_ANON_KEY`, `VITE_API_BASE_URL` — see [`../frontend/README.md`](../frontend/README.md#environment-variables-frontendenvlocal-never-committed).

External clients (MCP server, n8n) are set up separately — see [`../mcp/README.md`](../mcp/README.md) and [`../n8n/README.md`](../n8n/README.md).

## Rebuilding on a different machine

1. Clone the repo.
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. `dotnet restore` from `backend/` so that NuGet resolves everything.
4. Repeat the [Configure & run](#configure--run) step with that machine's own Supabase project and keys — user-secrets are per-machine, not checked into source control.
5. `dotnet build` then `dotnet run --project src/ChatApp.Api`.

No native/OS-specific dependencies — this builds and runs identically on Windows, macOS, and Linux wherever the .NET 10 SDK is installed.

## Project layout reference

| Folder | What lives here |
|---|---|
| `ChatApp.Domain/Entities` | `User`, `Conversation`, `Participant`, `Message` (+ `TextMessage`/`ImageMessage`), `ConversationMemory`, `ChunkMemory` |
| `ChatApp.Application/Features` | One folder per use case (`Command`/`Query` + `Handler` + `Validator`), grouped under `Conversations/`, `Messages/`, `Internal/` |
| `ChatApp.Application/Memory` | `ConversationMemoryService` — the rolling summarization pipeline |
| `ChatApp.Infrastructure/Persistence` | `AppDbContext` + EF Core entity configurations |
| `ChatApp.Infrastructure/Ai` | `GenerativeAiService` — the Gemini adapter behind `IGenerativeAiService` |
| `ChatApp.Api/Controllers` | Thin REST controllers — each action just forwards to `ISender.Send` |
| `ChatApp.Api/Realtime` | `ChatHub` (SignalR) and its notifier |
| `ChatApp.Api/Auth` | JWT + service-key authentication, `[AllowedRoles]` authorization |

## Related documents

- [Architecture & design](docs/backend-system-design-and-architecture.md)
- [Database design](docs/database-design.md) · [schema.sql](docs/schema.sql)
- [Requirements (SRS)](../docs/software-requirements-specification.md)
- [MCP integration](../mcp/README.md) · [n8n workflow](../n8n/README.md)
