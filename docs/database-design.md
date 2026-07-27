# Database Design

The data model. It is the concrete counterpart to `schema.sql`; every table, constraint, and trigger here maps 1:1 to that script. Technical usage is in `backend-system-design-and-architecture.md`; requirements in `software-requirements-specification.md`.

> **Principle.** Postgres is the single source of truth. Realtime only *notifies*. Conversations are **soft-deleted** (`is_deleted`), never dropped. AI outputs (OCR results) and the Agent reuse the message tables — the schema does not grow to add AI.

Engine: **PostgreSQL** (via **Supabase**). UUID keys, `timestamptz` timestamps, enums modeled as `CHECK` constraints. SQL columns are snake_case; the spec's PascalCase names map directly (e.g. `OwnerId → owner_id`, `Timestamp → sent_at`).

---

## Entity–relationship overview

```mermaid
erDiagram
    profiles ||--o{ participants : joins
    profiles ||--o{ messages : sends
    profiles |o--o{ conversations : owns
    conversations ||--o{ participants : has
    conversations ||--o{ messages : holds
    conversations ||--|| conversation_memory : owns
    conversations ||--o{ chunk_memories : accumulates
    messages ||--o| text_messages : "type=text"
    messages ||--o| image_messages : "type=image"
    messages ||--o| messages : replies_to

    profiles {
        uuid id PK
        text username UK
        text display_name
        bool is_agent
    }
    conversations {
        uuid id PK
        text public_id UK
        text display_name
        uuid owner_id FK "NULL = frozen"
        bool is_deleted
        bool is_readonly
        timestamptz last_message_time
    }
    participants {
        uuid conversation_id FK
        uuid user_id FK
    }
    messages {
        uuid id PK
        uuid conversation_id FK
        uuid user_id FK
        text type "text | image"
        uuid replies_to_message_id FK
        timestamptz sent_at
    }
    text_messages {
        uuid message_id PK
        text content
    }
    image_messages {
        uuid message_id PK
        text image_url
        text caption
        text ocr_status
        text ocr_content
    }
    conversation_memory {
        uuid conversation_id PK
        text global_memory
        int pending_tokens
    }
    chunk_memories {
        bigint id PK
        uuid conversation_id FK
        uuid start_message_id FK
        uuid end_message_id FK
        text memory
    }
```

Eight tables: users, conversations, participants, a table-per-type message trio, and two memory tables.

---

## Tables

### profiles (users)
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | uuid | PK | = Supabase auth id |
| `username` | text | **unique**, `~ '^[A-Za-z0-9]{1,30}$'` | letters + digits, ≤30 |
| `display_name` | text | not null | |
| `is_agent` | boolean | not null, default false | hidden OCR Agent |
| `created_time` | timestamptz | not null, default now() | |

### conversations
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | uuid | PK | |
| `public_id` | text | **unique**, `~ '^[A-Za-z0-9]{6}$'` | 6 chars, case-sensitive, backend-generated; used to join |
| `display_name` | text | not null | auto-generated at creation, owner-editable |
| `owner_id` | uuid | FK → profiles, on delete set null | **nullable & transferable**; `NULL` = **frozen** |
| `is_deleted` | boolean | not null, default false | soft delete |
| `is_readonly` | boolean | not null, default false | when true, only the owner may send |
| `created_time` | timestamptz | not null, default now() | |
| `last_message_time` | timestamptz | not null, default now() | trigger-maintained |

### participants
The Agent is **not** a participant, so participant counts are always human.

| Column | Type | Constraints |
|---|---|---|
| `conversation_id` | uuid | FK → conversations, on delete cascade |
| `user_id` | uuid | FK → profiles, on delete cascade |
| `joined_time` | timestamptz | not null, default now() |
| — | — | PK (`conversation_id`, `user_id`) |

### messages (base, table-per-type)
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | uuid | PK | |
| `conversation_id` | uuid | FK → conversations, on delete cascade | |
| `user_id` | uuid | FK → profiles, on delete cascade | sender (user or Agent) |
| `type` | text | check in (`text`,`image`) | **discriminator** |
| `replies_to_message_id` | uuid | FK → messages, on delete set null | OCR reply points to the image |
| `sent_at` | timestamptz | not null, default now() | spec's `Timestamp` |

### text_messages
| Column | Type | Constraints |
|---|---|---|
| `message_id` | uuid | PK, FK → messages, on delete cascade |
| `content` | text | not null |

### image_messages
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `message_id` | uuid | PK, FK → messages, on delete cascade | |
| `image_url` | text | not null | |
| `caption` | text | | AI caption generated on send (feeds memory) |
| `ocr_status` | text | check in (`NOT_REQUESTED`,`PROCESSING`,`FINISHED`,`TEXT_NOT_FOUND`) | set on send by text-detection |
| `ocr_content` | text | nullable | filled when OCR finishes |

### conversation_memory (1:1)
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `conversation_id` | uuid | PK, FK → conversations, on delete cascade | |
| `global_memory` | text | not null, default '' | rolling fold |
| `pending_tokens` | integer | not null, default 0, check ≥ 0 | threshold counter (backend-maintained) |
| `last_updated_time` | timestamptz | not null, default now() | |

### chunk_memories
| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | bigint | PK, generated always as identity | also the chunk order |
| `conversation_id` | uuid | FK → conversations, on delete cascade | |
| `start_message_id` / `end_message_id` | uuid | FK → messages, on delete set null | the chunk's message range |
| `memory` | text | not null | chunk summary |
| `created_time` | timestamptz | not null, default now() | |

The rolling **pointer** is implicit: the newest chunk's `end_message_id` marks where summarization last reached.

---

## Ownership & lifecycle rules

| Rule | Enforced by |
|---|---|
| `owner_id` = creator, but **transferable** and **nullable** | update allowed by owner (no immutability trigger) |
| `owner_id IS NULL` ⟺ **frozen** (no new joins; existing members may chat/leave) | `can_join()` requires `owner_id IS NOT NULL` |
| Owner leaving → **soft-delete** (`is_deleted = true`) or **freeze** (`owner_id = null`) | app command |
| Delete is **soft**; rows are retained | `is_deleted` flag |
| `is_readonly` auto-set when participants drop to 1, auto-cleared when a join brings it back to 2; owner may also set it manually | participant triggers (§ below) |

> **Readonly is a single flag** managed at the 1↔2 participant boundary. A brand-new conversation is created with ≥2 participants, so it is never persistently readonly at creation; the only way to reach 1 participant is by leaving, which the delete trigger handles.

---

## Relationships & cascade

FKs cascade from `conversations` to `participants`, `messages`, `conversation_memory`, and `chunk_memories`; `messages` cascade to `text_messages` / `image_messages`. Normal deletion is **soft** (`is_deleted`), so cascade is a defensive guarantee for the rare hard delete. Self-reference (`replies_to_message_id`) and memory pointers use `on delete set null`.

---

## Indexes

| Index | Columns | Serves |
|---|---|---|
| `idx_messages_conv_sent` | (`conversation_id`, `sent_at desc`) | history pagination |
| `idx_participants_user` | (`user_id`) | "which conversations am I in?" |
| `idx_conversations_last` | (`last_message_time desc`) | conversation list by recency |
| `idx_chunks_conv` | (`conversation_id`, `id`) | chunk ordering |

---

## Triggers

| Trigger | Fires | Guarantees |
|---|---|---|
| `conversations_after_insert` | after insert on conversations | owner auto-added as participant + 1:1 memory row created (SECURITY DEFINER) |
| `participants_after_insert` | after insert on participants | clears `is_readonly` when count reaches 2 |
| `participants_after_delete` | after delete on participants | sets `is_readonly` when count drops to ≤1 |
| `messages_after_insert` | after insert on messages | updates `last_message_time` |
| `on_auth_user_created` | after insert on auth.users | provisions a profile (Supabase only) |

The Agent is seeded once (`username = aiagent`, `is_agent = true`). **Note:** the previous immutable-`owner_id` trigger was removed — ownership is now transferable.

---

## Memory tables — deep dive

Hierarchical rolling summarization (mechanics in the architecture doc §6). Field mapping to the spec: `GlobalMemory → global_memory`, `PendingTokens → pending_tokens`, chunk `Memory → memory` with `StartMessageId`/`EndMessageId`.

- **`conversation_memory`** holds the live state: `global_memory` (evolving overall recap) and `pending_tokens` (accrued since the last chunk; the backend increments this per message — an image message counts the tokens of its `caption`).
- **`chunk_memories`** is the append-only, `id`-ordered history; each row's `start_message_id`/`end_message_id` bound its message range.

When `pending_tokens` crosses the configured threshold (or a summary is requested), the backend forms a new chunk over the pending messages, writes its `memory`, folds it into `global_memory`, and resets `pending_tokens`. An **on-demand summary** reads `global_memory` plus a fresh summary of messages after the newest chunk's `end_message_id` — never a full-history scan.

---

## Row-Level Security

RLS is enabled on all tables. Membership/ownership checks are delegated to **`SECURITY DEFINER`** helpers so policies on `participants` don't recurse:

```sql
is_participant(conv)  -- caller is in participants(conv)
is_owner(conv)        -- conversations(conv).owner_id = auth.uid()
is_readonly(conv)     -- conversations(conv).is_readonly (missing => true)
can_join(conv)        -- owner_id IS NOT NULL AND is_deleted = false  (i.e. not frozen/deleted)
```

Policy summary (`auth.uid()` = current user):

| Table | Select | Insert | Update | Delete |
|---|---|---|---|---|
| profiles | everyone | own row | own row | — |
| conversations | `is_participant(id)` & not deleted | `owner_id = auth.uid()` | `is_owner(id)` (rename / readonly / transfer / soft-delete / freeze) | — (soft delete via update) |
| participants | `is_participant(conversation_id)` | `is_owner(...)` **or** self-join (`user_id=auth.uid()` & `can_join`) | — | `is_owner(...)` **or** self-leave |
| messages | `is_participant(conversation_id)` | sender is self, participant, and not blocked by readonly (unless owner) | — | — |
| text_messages / image_messages | participant of parent's conversation | caller owns the parent message | service role (OCR/caption) | — |
| conversation_memory / chunk_memories | `is_participant(conversation_id)` | service role | service role | service role |

Background writes (memory worker) and Agent OCR/caption writes run with the **service role**, which bypasses RLS by design.

---

## Data lifecycle

- **Create conversation** (≥2 participants) → row with generated `public_id`; owner auto-membered; empty memory row.
- **Join** → user submits `public_id`; if the conversation is joinable (`can_join`), a `participants` row is added; if this makes count = 2, readonly auto-clears.
- **Send message** → base `messages` row + `text_messages`/`image_messages` child; `last_message_time` updated; backend adds token count. Image sends also set `caption` + `ocr_status` (`NOT_REQUESTED` or `TEXT_NOT_FOUND`) via one vision pass.
- **OCR** → `ocr_status` `NOT_REQUESTED → PROCESSING → FINISHED`; `ocr_content` filled; Agent posts a `text` reply (`replies_to_message_id` = image).
- **Transfer ownership** → `owner_id` updated to another participant.
- **Owner leaves** → soft-delete (`is_deleted = true`) or **freeze** (`owner_id = null`).
- **Participant leaves** → row removed; if count drops to 1, readonly auto-set.

---

## Mapping & verification

This design maps 1:1 to `schema.sql`. That script is **idempotent** and was verified on PostgreSQL 16: it runs twice cleanly, and functional tests confirm the username/`public_id` format checks, ownership **transfer** and **freeze** (`owner_id = null`), the readonly auto-toggle at the 1↔2 boundary, the message `type` discriminator and `ocr_status` domain, and the `last_message_time` trigger.
