-- =============================================================================
--  Realtime Chat App — Database Schema (PostgreSQL / Supabase)
--  Safe to run repeatedly: every object uses IF EXISTS / CREATE OR REPLACE.
--  Target: Supabase SQL editor. Core schema is portable to plain Postgres;
--          the auth->profile trigger and RLS policies are Supabase-native.
--  SQL uses snake_case; the mapping to the spec's PascalCase names is in
--  database-design.md (e.g. Timestamp -> sent_at, OwnerId -> owner_id).
-- =============================================================================

create extension if not exists pgcrypto;   -- gen_random_uuid()

-- -----------------------------------------------------------------------------
-- 1. TABLES
-- -----------------------------------------------------------------------------

-- Users. profiles.id equals auth.users.id in Supabase (see trigger in §4).
create table if not exists profiles (
    id            uuid primary key default gen_random_uuid(),
    username      text not null unique
                      check (username ~ '^[A-Za-z0-9]{1,30}$'),   -- letters+digits, <=30
    is_agent      boolean not null default false,                 -- hidden OCR Agent
    created_time  timestamptz not null default now()
);

-- Conversations. owner_id NULL == frozen (owner left & chose freeze).
create table if not exists conversations (
    id                uuid primary key default gen_random_uuid(),
    public_id         text not null unique
                          check (public_id ~ '^[A-Za-z0-9]{6}$'), -- 6 chars, case-sensitive, backend-generated
    display_name      text not null,
    owner_id          uuid references profiles(id) on delete set null,  -- NULL = frozen (transferable)
    is_deleted        boolean not null default false,             -- soft delete
    is_readonly       boolean not null default false,             -- only owner may send when true
    created_time      timestamptz not null default now(),
    last_message_time timestamptz not null default now()
);

-- Participants of a conversation (owner included). The Agent is NOT a participant.
create table if not exists participants (
    conversation_id uuid not null references conversations(id) on delete cascade,
    user_id         uuid not null references profiles(id)      on delete cascade,
    joined_time     timestamptz not null default now(),
    primary key (conversation_id, user_id)
);

-- Base message (table-per-type). `type` is the discriminator.
create table if not exists messages (
    id                     uuid primary key default gen_random_uuid(),
    conversation_id        uuid not null references conversations(id) on delete cascade,
    user_id                uuid not null references profiles(id)      on delete cascade,
    type                   text not null check (type in ('text','image')),
    replies_to_message_id  uuid references messages(id) on delete set null,
    sent_at                timestamptz not null default now()          -- spec: "Timestamp"
);

create table if not exists text_messages (
    message_id uuid primary key references messages(id) on delete cascade,
    content    text not null
);

create table if not exists image_messages (
    message_id  uuid primary key references messages(id) on delete cascade,
    image_url   text not null,
    caption     text,                                               -- AI caption (set on send)
    ocr_status  text not null default 'NOT_REQUESTED'
                    check (ocr_status in ('NOT_REQUESTED','PROCESSING','FINISHED','TEXT_NOT_FOUND')),
    ocr_content text
);

-- 1:1 rolling memory state per conversation.
create table if not exists conversation_memory (
    conversation_id   uuid primary key references conversations(id) on delete cascade,
    global_memory     text not null default '',
    pending_tokens    integer not null default 0 check (pending_tokens >= 0),
    last_updated_time timestamptz not null default now()
);

-- Per-chunk memories, ordered by the auto-increment id.
create table if not exists chunk_memories (
    id               bigint generated always as identity primary key,
    conversation_id  uuid not null references conversations(id) on delete cascade,
    start_message_id uuid references messages(id) on delete restrict,
    end_message_id   uuid references messages(id) on delete restrict,
    memory           text not null,
    created_time     timestamptz not null default now()
);

-- -----------------------------------------------------------------------------
-- 2. INDEXES
-- -----------------------------------------------------------------------------
create index if not exists idx_messages_conv_sent  on messages (conversation_id, sent_at desc);
create index if not exists idx_participants_user    on participants (user_id);
create index if not exists idx_chunks_conv          on chunk_memories (conversation_id, id);
-- conversation list ordered by recency
create index if not exists idx_conversations_last   on conversations (last_message_time desc);

-- -----------------------------------------------------------------------------
-- 3. TRIGGERS
-- -----------------------------------------------------------------------------

-- NOTE: create-time bookkeeping (adding the owner as a participant and creating
-- the 1:1 conversation_memory row) is owned by the APPLICATION layer, not a
-- trigger (decision A-3). Doing it in the handler keeps it testable without a
-- DB and visible in code; a trigger would add zero defensive value since only
-- the app ever creates a conversation. Any previously-defined
-- conversations_after_insert trigger/function is dropped for idempotency:
drop trigger  if exists conversations_after_insert on conversations;
drop function if exists trg_on_conversation_created();

-- 3b. Auto-manage is_readonly at the 1<->2 participant boundary.
--     -> 1 participant: set readonly.   -> back to 2: clear readonly.
create or replace function trg_participant_added()
returns trigger language plpgsql security definer set search_path = public as $$
begin
    if (select count(*) from participants where conversation_id = new.conversation_id) = 2 then
        update conversations set is_readonly = false
            where id = new.conversation_id and is_deleted = false;
    end if;
    return new;
end;
$$;

drop trigger if exists participants_after_insert on participants;
create trigger participants_after_insert
    after insert on participants
    for each row execute function trg_participant_added();

create or replace function trg_participant_removed()
returns trigger language plpgsql security definer set search_path = public as $$
begin
    if (select count(*) from participants where conversation_id = old.conversation_id) <= 1 then
        update conversations set is_readonly = true
            where id = old.conversation_id and is_deleted = false;
    end if;
    return old;
end;
$$;

drop trigger if exists participants_after_delete on participants;
create trigger participants_after_delete
    after delete on participants
    for each row execute function trg_participant_removed();

-- 3c. Keep last_message_time fresh on every new message.
create or replace function trg_message_inserted()
returns trigger language plpgsql security definer set search_path = public as $$
begin
    update conversations set last_message_time = new.sent_at
        where id = new.conversation_id;
    return new;
end;
$$;

drop trigger if exists messages_after_insert on messages;
create trigger messages_after_insert
    after insert on messages
    for each row execute function trg_message_inserted();

-- -----------------------------------------------------------------------------
-- 4. SUPABASE AUTH -> PROFILE  (skipped automatically on plain Postgres)
-- -----------------------------------------------------------------------------
create or replace function public.handle_new_user()
returns trigger language plpgsql security definer set search_path = public as $$
begin
    insert into public.profiles (id, username)
    values (
        new.id,
        coalesce(new.raw_user_meta_data->>'username', 'user' || left(replace(new.id::text,'-',''), 12))
    )
    on conflict (id) do nothing;
    return new;
end;
$$;

do $$
begin
    if exists (select 1 from information_schema.tables
               where table_schema = 'auth' and table_name = 'users') then
        drop trigger if exists on_auth_user_created on auth.users;
        create trigger on_auth_user_created
            after insert on auth.users
            for each row execute function public.handle_new_user();
    end if;
end $$;

-- -----------------------------------------------------------------------------
-- 5. SEED — the hidden system Agent (posts OCR replies)
-- -----------------------------------------------------------------------------
insert into profiles (id, username, is_agent)
values ('00000000-0000-0000-0000-000000000001', 'aiagent', true)
on conflict (id) do nothing;

-- -----------------------------------------------------------------------------
-- 6. ROW-LEVEL SECURITY  (Supabase: auth.uid() = current user)
--    Helpers are SECURITY DEFINER to avoid RLS recursion.
-- -----------------------------------------------------------------------------
create or replace function public.is_participant(conv uuid)
returns boolean language sql security definer stable set search_path = public as $$
    select exists (select 1 from participants
                   where conversation_id = conv and user_id = auth.uid());
$$;

create or replace function public.is_owner(conv uuid)
returns boolean language sql security definer stable set search_path = public as $$
    select exists (select 1 from conversations
                   where id = conv and owner_id = auth.uid());
$$;

create or replace function public.is_readonly(conv uuid)
returns boolean language sql security definer stable set search_path = public as $$
    select coalesce((select is_readonly from conversations where id = conv), true);
$$;

create or replace function public.can_join(conv uuid)
returns boolean language sql security definer stable set search_path = public as $$
    select exists (select 1 from conversations
                   where id = conv and owner_id is not null and is_deleted = false);
$$;

alter table profiles             enable row level security;
alter table conversations        enable row level security;
alter table participants         enable row level security;
alter table messages             enable row level security;
alter table text_messages        enable row level security;
alter table image_messages       enable row level security;
alter table conversation_memory  enable row level security;
alter table chunk_memories       enable row level security;

-- profiles: everyone reads (names); edit only your own
drop policy if exists profiles_select on profiles;
create policy profiles_select on profiles for select using (true);
drop policy if exists profiles_update on profiles;
create policy profiles_update on profiles for update
    using (auth.uid() = id) with check (auth.uid() = id);

-- conversations: participants read (non-deleted); creator inserts as owner;
--                owner updates (rename / readonly / transfer / soft-delete / freeze)
drop policy if exists conversations_select on conversations;
create policy conversations_select on conversations for select
    using (is_participant(id) and is_deleted = false);
drop policy if exists conversations_insert on conversations;
create policy conversations_insert on conversations for insert
    with check (owner_id = auth.uid());
drop policy if exists conversations_update on conversations;
create policy conversations_update on conversations for update
    using (is_owner(id));

-- participants: co-participants read; owner adds OR user self-joins a joinable conv;
--               owner removes OR user leaves
drop policy if exists participants_select on participants;
create policy participants_select on participants for select
    using (is_participant(conversation_id));
drop policy if exists participants_insert on participants;
create policy participants_insert on participants for insert
    with check (is_owner(conversation_id)
                or (user_id = auth.uid() and can_join(conversation_id)));
drop policy if exists participants_delete on participants;
create policy participants_delete on participants for delete
    using (is_owner(conversation_id) or user_id = auth.uid());

-- messages: participants read; a participant sends as self, blocked when readonly
--           (unless owner). Agent replies are inserted by the service role.
drop policy if exists messages_select on messages;
create policy messages_select on messages for select
    using (is_participant(conversation_id));
drop policy if exists messages_insert on messages;
create policy messages_insert on messages for insert
    with check (user_id = auth.uid()
                and is_participant(conversation_id)
                and (not is_readonly(conversation_id) or is_owner(conversation_id)));

-- text/image children: read if participant of the parent's conversation;
--   insert if you own the parent message. OCR/caption updates are service-role.
drop policy if exists text_select on text_messages;
create policy text_select on text_messages for select
    using (exists (select 1 from messages m
                   where m.id = message_id and is_participant(m.conversation_id)));
drop policy if exists text_insert on text_messages;
create policy text_insert on text_messages for insert
    with check (exists (select 1 from messages m
                        where m.id = message_id and m.user_id = auth.uid()));

drop policy if exists image_select on image_messages;
create policy image_select on image_messages for select
    using (exists (select 1 from messages m
                   where m.id = message_id and is_participant(m.conversation_id)));
drop policy if exists image_insert on image_messages;
create policy image_insert on image_messages for insert
    with check (exists (select 1 from messages m
                        where m.id = message_id and m.user_id = auth.uid()));

-- memory: participants read; writes are service-role (background worker)
drop policy if exists memory_select on conversation_memory;
create policy memory_select on conversation_memory for select
    using (is_participant(conversation_id));
drop policy if exists chunks_select on chunk_memories;
create policy chunks_select on chunk_memories for select
    using (is_participant(conversation_id));

-- =============================================================================
--  End of schema.
-- =============================================================================
