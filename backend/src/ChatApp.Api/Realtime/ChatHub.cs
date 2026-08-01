using ChatApp.Api.Auth;
using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Realtime;

/// <summary>
/// SignalR hub for realtime chat. <see cref="SendMessage"/>/<see cref="SendImage"/> dispatch through
/// <see cref="ISender"/>, then fire the memory update via <see cref="DetachedMemoryUpdateDispatcher"/> —
/// never awaited, so the hub method returns as soon as the send itself completes.
/// </summary>
[Authorize]
public sealed class ChatHub(
    ISender sender,
    ICurrentUserProvider currentUserProvider,
    IAppDbContext db,
    IUserConnectionTracker connectionTracker,
    DetachedMemoryUpdateDispatcher memoryDispatcher) : Hub
{
    /// <summary>Adds the connection to a Group per conversation the caller currently participates in.</summary>
    public override async Task OnConnectedAsync()
    {
        currentUserProvider.Principal = Context.User;

        if (TryGetCallerId(out var userId))
        {
            connectionTracker.Add(userId, Context.ConnectionId);

            var conversationIds = await db.ToListAsync(
                db.Participants.Where(p => p.UserId == userId).Select(p => p.ConversationId),
                Context.ConnectionAborted);

            foreach (var conversationId in conversationIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConversationNotifier.GroupName(conversationId));
            }
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetCallerId(out var userId))
        {
            connectionTracker.Remove(userId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Sends a text message; fires the detached memory update on success, counting <paramref name="text"/>'s tokens.</summary>
    public async Task SendMessage(Guid conversationId, string text)
    {
        currentUserProvider.Principal = Context.User;

        var result = await sender.Send(new Application.Features.Messages.Send.Command(conversationId, text));
        ThrowIfFailed(result);

        memoryDispatcher.FireAndForget(conversationId, text);
    }

    /// <summary>Sends an image message; fires the detached memory update on success, counting the server-generated caption's tokens.</summary>
    public async Task SendImage(Guid conversationId, string imageUrl)
    {
        currentUserProvider.Principal = Context.User;

        var result = await sender.Send(new Application.Features.Messages.SendImage.Command(conversationId, imageUrl));
        ThrowIfFailed(result);

        memoryDispatcher.FireAndForget(conversationId, result.Value!.Caption ?? string.Empty);
    }

    private bool TryGetCallerId(out Guid userId)
    {
        if (Context.User?.FindFirst(ClientClaimTypes.Subject) is { Value: var value } && Guid.TryParse(value, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }

    private static void ThrowIfFailed<T>(Result<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new HubException($"{result.Error!.Code}: {result.Error.Message}");
        }
    }
}
