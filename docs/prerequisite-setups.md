# Prerequisite Setups

A from-scratch guide to run the project locally: tools → Supabase → database → backend → frontend → verify. Design context is in `backend-system-design-and-architecture.md`; the schema is `schema.sql`.

---

## 0. What you will set up

```mermaid
flowchart LR
    Tools["1. Tools"] --> Supa["2. Supabase<br/>(Auth · Storage · Postgres)"]
    Supa --> DB["3. Apply schema.sql"]
    DB --> BE["4. Backend (.NET)"]
    BE --> FE["5. Frontend (React PWA)"]
    FE --> Verify["6. Verify end-to-end"]
```

---

## 1. Tools to install

| Tool | Version | Purpose | Link |
|---|---|---|---|
| .NET SDK | **10.x** | Build & run the backend | https://dotnet.microsoft.com/download |
| Node.js + npm | **≥ 20 LTS** | Build & run the frontend PWA | https://nodejs.org |
| Docker Desktop | latest | Local Supabase stack (Postgres/Auth/Storage) | https://www.docker.com/products/docker-desktop |
| Supabase CLI | latest | Run Supabase locally / manage projects | https://supabase.com/docs/guides/cli |
| Git | latest | Version control | https://git-scm.com |

You also need a **Google AI Studio API key** for Gemini: https://aistudio.google.com/apikey

Verify:
```bash
dotnet --version   # 10.x
node --version     # v20+
docker --version
supabase --version
```

---

## 2. Supabase

Two options — **Option A (local)** is recommended for development.

### Option A — Local stack via Supabase CLI (Docker)
Runs Postgres + Auth + Storage + Studio on your machine.
```bash
supabase init            # creates ./supabase in the repo
supabase start           # boots the full stack in Docker
```
`supabase start` prints your local credentials — copy these:
- **API URL** (e.g. `http://127.0.0.1:54321`)
- **anon key** and **service_role key**
- Studio URL (e.g. `http://127.0.0.1:54323`)

> You do **not** need to copy a JWT secret — see the note below on how the backend validates tokens.

Docs: https://supabase.com/docs/guides/local-development

### Option B — Hosted project (for staging / sharing)
1. Create a project at https://supabase.com/dashboard.
2. **Project Settings → API**: copy the **Project URL**, **anon** key, and **service_role** key.

> You do **not** need to copy a JWT secret — see the note below.

### Storage bucket (both options)
Create a bucket named **`images`** for chat images (Studio → Storage → New bucket). Keep it private. **The client (frontend) uploads images directly and issues its own signed URLs** via the Supabase client — the backend never streams image bytes on the message path. The backend's `IStorageClient` has a single job: **download an object's bytes with the service-role key** so the Gemini vision call (caption / OCR) can read a private image without depending on a client-supplied signed URL that may have expired. Docs: https://supabase.com/docs/guides/storage

### Auth (both options) — Google OAuth only
This app has **no email/password sign-in** — Google is the only provider. Set it up in two places:

**1. Google Cloud Console** (create OAuth credentials):
1. Go to [console.cloud.google.com](https://console.cloud.google.com), create/select a project.
2. **APIs & Services → OAuth consent screen** — choose **External**, fill in the required fields (app name, support email). For local dev/testing this can stay in "Testing" mode.
3. **APIs & Services → Credentials → Create Credentials → OAuth Client ID** → Application type **Web application**.
4. **Authorized JavaScript origins**: your frontend's URL (e.g. `http://localhost:5173` for dev, plus your production origin later).
5. **Authorized redirect URIs**: your Supabase project's callback URL —
   - Hosted: `https://<project-ref>.supabase.co/auth/v1/callback`
   - Local CLI: `http://127.0.0.1:54321/auth/v1/callback`
6. Create, then copy the **Client ID** and **Client Secret**.

**2. Supabase** (enable the provider):
- **Hosted (Option B):** Studio → **Authentication → Providers → Google** → toggle on, paste the Client ID + Secret → Save. Then **Authentication → URL Configuration**: set **Site URL** and add your frontend origin(s) to **Redirect URLs**.
- **Local CLI (Option A):** add to `supabase/config.toml`:
  ```toml
  [auth.external.google]
  enabled = true
  client_id = "<client id>"
  secret = "env(SUPABASE_AUTH_EXTERNAL_GOOGLE_SECRET)"
  ```
  and put the real secret in `supabase/.env` as `SUPABASE_AUTH_EXTERNAL_GOOGLE_SECRET=...`, then `supabase stop && supabase start` to pick it up.

The frontend calls `supabase.auth.signInWithOAuth({ provider: 'google' })` — no custom sign-up form, no password field. **Username and email are derived automatically** by the `handle_new_user` trigger in `schema.sql` (email → username local-part, sanitized, deduplicated) — nothing to configure for that part.

Reference: [Login with Google](https://supabase.com/docs/guides/auth/social-login/auth-google) · [Supabase social login overview](https://supabase.com/docs/guides/auth/social-login)

> **How the backend validates tokens (no static secret needed).** Supabase Auth now defaults new projects — and recent Supabase CLI versions default local dev too — to **asymmetric JWT signing** (ES256), not the legacy shared HS256 secret. Copying a "JWT secret" and validating locally against it (the old approach) can silently stop matching the actual signing key. Instead, the **Api** validates tokens against Supabase's **JWKS endpoint** (`{Supabase:Url}/auth/v1/.well-known/jwks.json` — confirm the exact path for your Supabase version), which exposes the current public signing key(s) and works whether the project uses the new asymmetric keys or the legacy HS256 secret (Supabase publishes the legacy secret there too, as a symmetric JWK, for backward compatibility). This needs only `Supabase:Url` — no separate secret to copy or store. Reference: [JWT Signing Keys](https://supabase.com/docs/guides/auth/signing-keys).

---

## 3. Apply the database schema

Run `schema.sql` against your Supabase database.

- **Studio:** open the **SQL Editor**, paste `schema.sql`, run. It is idempotent (safe to re-run).
- **CLI (local):**
  ```bash
  supabase db reset            # optional: clean slate
  psql "$DATABASE_URL" -f schema.sql   # DATABASE_URL from `supabase status`
  ```

This creates the 8 tables, triggers, the seeded AI Agent, and the RLS policies. See `database-design.md` for the model.

---

## 4. Backend (.NET)

```bash
git clone <your-repo-url> && cd <repo>/backend
dotnet restore
dotnet build
```

Configure secrets with **user-secrets** (never commit keys):
```bash
cd src/ChatApp.Api
dotnet user-secrets init
dotnet user-secrets set "Supabase:Url"            "<API URL>"
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service_role key>"
dotnet user-secrets set "Supabase:StorageBucket"  "images"
dotnet user-secrets set "ConnectionStrings:Postgres" "<Postgres connection string — see note below>"
dotnet user-secrets set "Gemini:ApiKey"           "<google ai studio key>"
dotnet user-secrets set "Gemini:Model"            "gemini-3.5-flash-lite"
dotnet user-secrets set "Clients:McpKey"          "<random secret for MCP client>"
dotnet user-secrets set "Clients:N8nKey"          "<random secret for n8n client>"
dotnet user-secrets set "Memory:TokenThreshold"   "1500"
```

> **`ConnectionStrings:Postgres` is a *different credential* from `Supabase:ServiceRoleKey` — do not confuse them.** `ServiceRoleKey` is a **JWT**, used as a Bearer token for Supabase's REST APIs (Storage, PostgREST). EF Core's `AppDbContext` connects to Postgres directly via Npgsql, which needs a **real Postgres role and password** — get this from Supabase **Project Settings → Database → Connection string** (local CLI: printed by `supabase status`). Use the `postgres` role or the `service_role` DB role (both have `BYPASSRLS`); using the JWT as a password will simply fail to authenticate. This is also the role that makes §11's RLS-bypass guarantee true — connecting with an RLS-subject role instead makes `auth.uid()` NULL and every query returns zero rows silently.
>
> **For a hosted project, pick the "Session Pooler" connection string, not "Direct connection".** Direct connections are IPv6-only on most hosted Supabase projects, which fails to connect from many networks/hosts; the Transaction Pooler mode also conflicts with Npgsql's default prepared-statement caching under EF Core. Session Pooler avoids both pitfalls.

Run:
```bash
dotnet run --project src/ChatApp.Api    # API + SignalR, e.g. https://localhost:5001
```

Notes:
- The schema is applied via `schema.sql` (§3), not EF migrations — EF Core here is read/write mapping only.
- If you added the licensed MediatR (v13+), set its license key per §4.1 of the backend doc; otherwise use a free mediator package.

Reference: [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) · [.NET config](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/)

---

## 5. Frontend (React PWA)

```bash
cd ../frontend
npm install
```

Create `.env.local`:
```bash
VITE_SUPABASE_URL=<API URL>
VITE_SUPABASE_ANON_KEY=<anon key>
VITE_API_BASE_URL=https://localhost:5001
```

Run:
```bash
npm run dev            # http://localhost:5173
```
On mobile, open the dev URL and choose **Add to Home Screen** to install the PWA.

---

## 6. Verify end-to-end

1. Sign in with two different Google accounts (two browsers/profiles, or one regular + one incognito) — usernames are auto-derived, nothing to type.
2. User A creates a conversation adding User B **by username** → note the generated `PublicId`.
3. Send text messages both ways — they appear in real time.
4. Send an image → it renders, and gets an AI-generated caption (feeds conversation memory). There is no text-extraction/OCR feature — captioning is the only AI step an image goes through.
5. (Optional) User C joins with the `PublicId`.

If all five pass, the environment is correctly wired.

---

## 7. Environment variables — quick reference

**Backend** (`src/ChatApp.Api`, via user-secrets or environment):

| Key | Meaning |
|---|---|
| `Supabase:Url` | Supabase API URL |
| `Supabase:ServiceRoleKey` | Service role key (server-only). The backend **must** use a role that bypasses RLS — with an RLS-subject role, `auth.uid()` is `NULL` and every query returns zero rows silently |
| `ConnectionStrings:Postgres` | The **actual Postgres connection string** (Project Settings → Database), used by `AppDbContext`/Npgsql. Not the same credential as `ServiceRoleKey` — that's a JWT for REST APIs, not a DB password |

| `Supabase:StorageBucket` | `images` |
| `Gemini:ApiKey` / `Gemini:Model` | Google AI Studio key / `gemini-3.5-flash-lite` (verified working; **Gemini model IDs churn fast** — Google has retired several 2.x models ahead of their published shutdown dates. If you hit a 404 on the configured model, check https://ai.google.dev/gemini-api/docs/deprecations and update `Gemini:Model` — no code change needed, it's config-driven.) |
| `Clients:McpKey` / `Clients:N8nKey` | Service keys mapping callers to `Mcp` / `N8n` client types |
| `Memory:TokenThreshold` | Tokens before a memory chunk is formed |

**Frontend** (`.env.local`): `VITE_SUPABASE_URL`, `VITE_SUPABASE_ANON_KEY`, `VITE_API_BASE_URL`.

> External clients (MCP server, n8n) are set up separately — see `mcp-integration.md` and `n8n-workflow.md`.
