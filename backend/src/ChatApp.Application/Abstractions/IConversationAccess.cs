using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the caller's resolved identity, set by the Api layer's authentication step (§4.2)
/// from the JWT (App), the on-behalf-of header (Mcp), or the service key alone (N8n) — the latter
/// carries no user identity, so <see cref="UserId"/> is null for an N8n caller. Also carries the two
/// conversation-access checks that depend on that identity, so handlers don't need to re-query
/// membership/ownership themselves. Client-type authorization (which caller kinds may reach a given
/// endpoint) is an Api-layer concern (<c>[AllowedClients]</c>, §4.2) and is not exposed here —
/// consequently, a handler must never infer a client type from <see cref="UserId"/> being null (e.g.
/// treating it as "this must be n8n"); it is simply "no identity", handled like any other precondition.
/// </summary>
public interface IConversationAccess
{
    /// <summary>The caller's resolved user id; null when the caller carries no user identity (e.g. n8n).</summary>
    Guid? UserId { get; }

    /// <summary>
    /// Resolves the caller's full user row. Fails with <see cref="ErrorType.Unexpected"/> if
    /// <see cref="UserId"/> is null or does not resolve to an existing profile — by the time a
    /// handler needs the full row, <c>[AllowedClients]</c> (§4.2) has already confirmed the caller is
    /// a user-carrying client, so a missing identity here is a configuration defect, not a normal
    /// caller-facing rejection. Use this only when more than the id is needed (e.g. <c>Username</c>
    /// for display-name generation); prefer <see cref="UserId"/> directly otherwise.
    /// </summary>
    Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// is its current owner. Fails with <see cref="ErrorType.Forbidden"/> if the caller has no
    /// identity or isn't the owner, or <see cref="ErrorType.NotFound"/> if the conversation doesn't
    /// exist (or is deleted). Backs the owner-only commands (Rename, SetReadonly, TransferOwnership,
    /// AddParticipants, RemoveParticipants).
    /// </summary>
    Task<Result<Conversation>> GetOwnedConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// may send into it: must be a participant, and — when the conversation is read-only — must be
    /// its owner (F-4). Fails with <see cref="ErrorType.Forbidden"/>/<see cref="ErrorType.NotFound"/>/
    /// <see cref="ErrorType.Conflict"/> accordingly. Backs <c>Send</c>/<c>SendImage</c>.
    /// </summary>
    Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken);
}
