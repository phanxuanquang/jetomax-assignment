using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the caller's resolved identity, set by the Api layer's authentication step (§4.2)
/// from the JWT (App) or the on-behalf-of header (Mcp/N8n) — every call, from every channel, now
/// resolves to a real authenticated user before a handler runs, so <see cref="UserId"/>/<see cref="Role"/>
/// are never absent in practice. Also carries the two conversation-access checks that depend on that
/// identity, so handlers don't need to re-query membership/ownership themselves. Role-based
/// authorization (which roles may reach a given endpoint) is an Api-layer concern (<c>[AllowedRoles]</c>,
/// §4.2) and is not enforced here — <see cref="Role"/> is exposed for handlers/Api to read, not for this
/// port to gate on.
/// </summary>
public interface IConversationAccess
{
    /// <summary>The caller's resolved user id.</summary>
    Guid UserId { get; }

    /// <summary>The caller's resolved system-wide role, resolved fresh per request (never cached in a token) so a mid-session demotion takes effect immediately.</summary>
    UserRole Role { get; }

    /// <summary>
    /// Resolves the caller's full user row. Fails with <see cref="ErrorType.Unexpected"/> if
    /// <see cref="UserId"/> does not resolve to an existing profile — a missing identity here is a
    /// configuration defect, not a normal caller-facing rejection. Use this only when more than the id
    /// is needed (e.g. <c>Username</c> for display-name generation); prefer <see cref="UserId"/> directly otherwise.
    /// </summary>
    Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// is its current owner. Fails with <see cref="ErrorType.Forbidden"/> if the caller isn't the
    /// owner, or <see cref="ErrorType.NotFound"/> if the conversation doesn't exist (or is deleted).
    /// Backs the owner-only commands (Rename, SetReadonly, TransferOwnership, AddParticipants, RemoveParticipants).
    /// </summary>
    Task<Result<Conversation>> GetOwnedConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// may send into it: must be a participant, and — when the conversation is read-only — must be
    /// its owner (F-4). Fails with <see cref="ErrorType.Forbidden"/>/<see cref="ErrorType.NotFound"/>/
    /// <see cref="ErrorType.Conflict"/> accordingly. Backs <c>Send</c>/<c>SendImage</c>.
    /// </summary>
    Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
