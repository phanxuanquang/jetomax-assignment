ASP.NET Core (.NET 10) backend for a realtime chat app: REST + SignalR API, Clean Architecture, backed by Supabase (Auth, Storage, Postgres) and Google Gemini for AI features. For the full system design, requirements, and data model, see [docs/](../docs/).

## Projects

```
backend/
├── ChatApp.sln
└── src/
    ├── ChatApp.Domain/          entities, enums — no dependencies
    ├── ChatApp.Application/     MediatR use cases + ports (interfaces)
    ├── ChatApp.Infrastructure/  EF Core, Supabase Storage, Semantic Kernel/Gemini
    └── ChatApp.Api/             host: controllers, SignalR Hub, auth, DI
```

## Prerequisites

- [.NET SDK 10.x](https://dotnet.microsoft.com/download)
- A Supabase project (local via Docker, or hosted) with `schema.sql` applied
- A Google AI Studio API key for Gemini

Full first-time setup (Supabase, Google OAuth, environment variables) is in [prerequisite-setups.md](../docs/prerequisite-setups.md) — follow that end-to-end on a fresh machine. The quick version below assumes Supabase is already set up.

## Build

Replace the [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) values in the following script with the actual values before executing:

```bash
cd backend
dotnet restore
dotnet build

cd src/ChatApp.Api
dotnet user-secrets init
dotnet user-secrets set "Supabase:Url"               "<API URL>"
dotnet user-secrets set "Supabase:ServiceRoleKey"    "<service_role key>"
dotnet user-secrets set "ConnectionStrings:Postgres" "<Postgres connection string>"
dotnet user-secrets set "Gemini:ApiKey"              "<google ai studio key>"
dotnet user-secrets set "Clients:McpKey"             "<random secret>"
dotnet user-secrets set "Clients:N8nKey"             "<random secret>"

dotnet run --project src/ChatApp.Api
```

Full variable list, what each one means, and common pitfalls (wrong pooler mode, service-role vs. connection-string confusion): [docs/prerequisite-setups.md §7](../docs/prerequisite-setups.md#7-environment-variables--quick-reference).

In `Development`, the API exposes a Scalar API reference (OpenAPI-based) for interactive exploration — check the console output for the exact URL on startup.

## Rebuilding on a different machine

1. Clone the repo.
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. `dotnet restore` from `backend/` so that NuGet resolves everything.
4. Repeat the [Configure secrets](#configure-secrets) step with that machine's own Supabase project and keys — user-secrets are per-machine, not checked into source control.
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

- [Architecture & design](../docs/backend-system-design-and-architecture.md)
- [Requirements (SRS)](../docs/software-requirements-specification.md)
- [Database design](../docs/database-design.md)
- [Prerequisite setup guide](../docs/prerequisite-setups.md)
- [MCP integration](../docs/mcp-integration.md) · [n8n workflow](../docs/n8n-workflow.md)
