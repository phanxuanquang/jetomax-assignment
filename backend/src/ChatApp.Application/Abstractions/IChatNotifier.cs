using ChatApp.Application.Features.Conversations;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto realtime broadcast (§5). Postgres is the source of truth; these calls only notify
/// connected clients so they can update live — a reconnecting client always re-fetches from REST/queries.
/// </summary>
public interface IChatNotifier
{
    /// <summary>Broadcasts a newly persisted message to every member of <paramref name="conversationId"/>.</summary>
    Task NotifyNewMessageAsync(Guid conversationId, Message message, CancellationToken cancellationToken);

    /// <summary>Broadcasts that a conversation's membership changed.</summary>
    Task NotifyMemberChangedAsync(Guid conversationId, Guid userId, MemberChangeAction action, CancellationToken cancellationToken);

    /// <summary>
    /// Broadcasts that collaborative OCR has started on <paramref name="imageMessageId"/>, so every
    /// client permanently disables its "Extract text" button for this image.
    /// </summary>
    Task NotifyOcrStartedAsync(Guid imageMessageId, CancellationToken cancellationToken);

    /// <summary>
    /// Broadcasts that collaborative OCR finished on <paramref name="imageMessageId"/>; the Agent's
    /// reply message has already been broadcast separately via <see cref="NotifyNewMessageAsync"/>.
    /// </summary>
    Task NotifyOcrDoneAsync(Guid imageMessageId, CancellationToken cancellationToken);

    /// <summary>
    /// Relays an n8n-published digest to whatever page/channel displays it. The backend does not
    /// persist the digest — this is a pure relay.
    /// </summary>
    Task NotifyDigestPublishedAsync(string digest, DateTime publishedAt, CancellationToken cancellationToken);
}
