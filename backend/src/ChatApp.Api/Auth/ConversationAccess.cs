using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;

namespace ChatApp.Api.Auth;

/// <summary>
/// Implements <see cref="IConversationAccess"/> over the caller's resolved identity
/// (<see cref="ICurrentUserProvider"/>) and <see cref="IAppDbContext"/> queries. <see cref="UserId"/>
/// reads the <see cref="ClientClaimTypes.Subject"/> claim set by whichever authentication scheme
/// handled the request (the Supabase JWT's own <c>sub</c> for App, or the validated
/// <c>X-On-Behalf-Of</c> header for Mcp) — null for N8n, which carries no user identity by design.
/// </summary>
public sealed class ConversationAccess(ICurrentUserProvider currentUserProvider, IAppDbContext db) : IConversationAccess
{
    public Guid? UserId =>
        currentUserProvider.Principal?.FindFirst(ClientClaimTypes.Subject) is { Value: var value } &&
        Guid.TryParse(value, out var userId)
            ? userId
            : null;

    public async Task<Result<User>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (UserId is not { } userId)
        {
            return Result<User>.Failure(Error.Unexpected("caller.identity_required", "This action requires a signed-in user."));
        }

        var user = await db.FirstOrDefaultAsync(db.Users.Where(u => u.Id == userId), cancellationToken);
        if (user is null)
        {
            return Result<User>.Failure(Error.Unexpected("caller.identity_required", "The caller's resolved identity does not correspond to an existing profile."));
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<Conversation>> GetOwnedConversationAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        if (UserId is not { } userId)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.owner_only", "This action requires a signed-in user."));
        }

        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.Id == conversationId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result<Conversation>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        if (conversation.OwnerId != userId)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.owner_only", "Only the conversation's owner may perform this action."));
        }

        return Result<Conversation>.Success(conversation);
    }

    public async Task<Result<Conversation>> EnsureCanSendAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        if (UserId is not { } userId)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.send.not_participant", "This action requires a signed-in user."));
        }

        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.Id == conversationId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result<Conversation>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        var isParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == conversationId && p.UserId == userId),
            cancellationToken);

        if (!isParticipant)
        {
            return Result<Conversation>.Failure(Error.Forbidden("conversation.send.not_participant", "The caller is not a participant of this conversation."));
        }

        if (conversation.IsReadonly && conversation.OwnerId != userId)
        {
            return Result<Conversation>.Failure(Error.Conflict("conversation.send.readonly", "This conversation is read-only; only the owner may send."));
        }

        return Result<Conversation>.Success(conversation);
    }
}
