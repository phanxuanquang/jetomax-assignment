using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;

namespace ChatApp.Api.Auth;

/// <summary>
/// Implements <see cref="IConversationAccess"/> over the caller's resolved identity and DB queries.
/// <see cref="UserId"/>/<see cref="Role"/> read claims the authentication pipeline guarantees exist, so
/// a missing claim here is a configuration defect, not a normal caller-facing outcome.
/// </summary>
public sealed class ConversationAccess(ICurrentUserProvider currentUserProvider, IAppDbContext db) : IConversationAccess
{
    public Guid UserId =>
        currentUserProvider.Principal?.FindFirst(ClientClaimTypes.Subject) is { Value: var value } &&
        Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("No resolved user id on the current principal; the authentication pipeline should have guaranteed one.");

    public UserRole Role =>
        currentUserProvider.Principal?.FindFirst(ClientClaimTypes.Role) is { Value: var value } &&
        Enum.TryParse<UserRole>(value, out var role)
            ? role
            : throw new InvalidOperationException("No resolved role on the current principal; the authentication pipeline should have guaranteed one.");

    public async Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.FirstOrDefaultAsync(db.Users.Where(u => u.Id == UserId), cancellationToken);
        if (user is null)
        {
            return Result<User>.Failure(Error.Unexpected("caller.identity_required", "The caller's resolved identity does not correspond to an existing profile."));
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<Conversation>> GetOwnedConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.Id == conversationId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result<Conversation>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        if (conversation.OwnerId != UserId)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.owner_only", "Only the conversation's owner may perform this action."));
        }

        return Result<Conversation>.Success(conversation);
    }

    public async Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.Id == conversationId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result<Conversation>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        var isParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == conversationId && p.UserId == UserId),
            cancellationToken);

        if (!isParticipant)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.send.not_participant", "The caller is not a participant of this conversation."));
        }

        if (conversation.IsReadonly && conversation.OwnerId != UserId)
        {
            return Result<Conversation>.Failure(Error.Conflict("conversation.send.readonly", "This conversation is read-only; only the owner may send."));
        }

        return Result<Conversation>.Success(conversation);
    }
}
