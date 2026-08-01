using ChatApp.Api.Auth;
using ChatApp.Api.DTOs;
using ChatApp.Api.Extensions;
using ChatApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConversationsFeature = ChatApp.Application.Features.Conversations;
using MessagesFeature = ChatApp.Application.Features.Messages;

namespace ChatApp.Api.Controllers;

/// <summary>
/// Thin REST surface over the <c>Conversations</c>/<c>Messages</c> Application slices; every action
/// just forwards to the matching command/query via <see cref="ISender"/>, no business logic here. Only
/// <see cref="Summarize"/> carries <c>[AllowedRoles]</c>, since it's the sole action narrower than any
/// authenticated role.
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.Get.Query(q), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.Create.Command(request.ParticipantUsernames), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinConversationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.Join.Command(request.PublicId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/name")]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameConversationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.Rename.Command(id, request.DisplayName), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/readonly")]
    public async Task<IActionResult> SetReadonly(Guid id, [FromBody] SetReadonlyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.SetReadonly.Command(id, request.IsReadonly), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.TransferOwnership.Command(id, request.NewOwnerUsername), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] Guid? before, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Get.Query(id, before, limit), cancellationToken);
        return result.ToActionResult();
    }
    [HttpGet("{id:guid}/messages/search")]
    public async Task<IActionResult> SearchMessages(Guid id, [FromQuery] string? q, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Search.Query(id, q ?? string.Empty, limit), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipants(Guid id, [FromBody] ParticipantsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.AddParticipants.Command(id, request.Usernames), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}/participants")]
    public async Task<IActionResult> RemoveParticipants(Guid id, [FromBody] ParticipantsRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.RemoveParticipants.Command(id, request.Usernames), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, [FromBody] LeaveConversationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ConversationsFeature.Leave.Command(id, request.Mode), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/summary")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> Summarize(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ChatApp.Application.Features.Internal.SummarizeConversation.Query(id), cancellationToken);
        return result.ToActionResult();
    }
}
