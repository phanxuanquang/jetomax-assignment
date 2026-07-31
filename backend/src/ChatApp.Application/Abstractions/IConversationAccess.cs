using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The caller's resolved identity, set by the Api layer's authentication step, plus the two
/// conversation-access checks that depend on it. Role-based authorization (<c>[AllowedRoles]</c>) is
/// an Api-layer concern, not enforced here — <see cref="Role"/> is exposed for reading, not for this
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
    /// its owner. Fails with <see cref="ErrorType.Forbidden"/>/<see cref="ErrorType.NotFound"/>/
    /// <see cref="ErrorType.Conflict"/> accordingly. Backs <c>Send</c>/<c>SendImage</c>.
    /// </summary>
    Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
