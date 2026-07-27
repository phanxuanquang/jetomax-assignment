using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the caller's resolved identity, set by the Api layer's authentication step (§4.2)
/// from the JWT (App), the on-behalf-of header (Mcp), or the service key alone (N8n). Also carries
/// the two conversation-access checks that depend on that identity, so handlers don't need to
/// resolve the caller's id and re-query membership/ownership themselves. Client-type authorization
/// (which caller kinds may reach a given endpoint) is an Api-layer concern (<c>[AllowedClients]</c>,
/// §4.2) and is not exposed here.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Resolves the caller's full user row. Fails with <see cref="ErrorType.Forbidden"/> if the
    /// caller carries no user identity (e.g. n8n) or no matching profile row exists.
    /// </summary>
    Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// is its current owner. Fails with <see cref="ErrorType.Forbidden"/> if the caller has no
    /// identity or isn't the owner, or <see cref="ErrorType.NotFound"/> if the conversation doesn't
    /// exist (or is deleted). Backs the owner-only commands (Rename, SetReadonly, TransferOwnership,
    /// AddParticipant, RemoveParticipant).
    /// </summary>
    Task<Result<Conversation>> GetOwnedConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the non-deleted conversation <paramref name="conversationId"/> and confirms the caller
    /// may send into it: must be a participant, and — when the conversation is read-only — must be
    /// its owner (F-4). Fails with <see cref="ErrorType.Forbidden"/>/<see cref="ErrorType.NotFound"/>/
    /// <see cref="ErrorType.Conflict"/> accordingly. Backs <c>SendMessage</c>/<c>SendImage</c>.
    /// </summary>
    Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken);
}
