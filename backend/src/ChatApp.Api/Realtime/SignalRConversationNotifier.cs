using ChatApp.Application.Abstractions;
using ChatApp.Application.Features.Conversations;
using ChatApp.Application.Features.Messages;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Realtime;

public sealed class SignalRConversationNotifier(IHubContext<ChatHub> hubContext, IUserConnectionTracker connectionTracker)
    : IConversationNotifier
{
    /// <summary>The SignalR Group name for <paramref name="conversationId"/>.</summary>
    public static string GroupName(Guid conversationId) => conversationId.ToString();

    public Task NotifyNewMessageAsync(Guid conversationId, Message message, CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(GroupName(conversationId)).SendAsync("NewMessage", MessageMapper.ToDto(message), cancellationToken);

    public async Task NotifyMemberChangedAsync(Guid conversationId, Guid userId, MemberChangeAction action, CancellationToken cancellationToken = default)
    {
        var group = GroupName(conversationId);

        // Keep an already-connected user's live sockets in sync with membership immediately, since
        // there's no client-facing "subscribe" hub method to do this from the frontend.
        foreach (var connectionId in connectionTracker.GetConnections(userId))
        {
            if (action == MemberChangeAction.Joined)
            {
                await hubContext.Groups.AddToGroupAsync(connectionId, group, cancellationToken);
            }
            else
            {
                await hubContext.Groups.RemoveFromGroupAsync(connectionId, group, cancellationToken);
            }
        }

        await hubContext.Clients.Group(group).SendAsync("MemberChanged", conversationId, userId, ToWireAction(action), cancellationToken);
    }

    public Task NotifyDigestPublishedAsync(string digest, DateTime publishedAt, CancellationToken cancellationToken = default) =>
        hubContext.Clients.All.SendAsync("DigestPublished", digest, publishedAt, cancellationToken);

    /// <summary>Maps <see cref="MemberChangeAction"/> to its wire values (<c>Added</c>/<c>Left</c>).</summary>
    private static string ToWireAction(MemberChangeAction action) => action switch
    {
        MemberChangeAction.Joined => "Added",
        MemberChangeAction.Left => "Left",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unmapped MemberChangeAction value.")
    };
}
