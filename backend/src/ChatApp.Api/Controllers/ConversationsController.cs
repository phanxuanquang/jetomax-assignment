using ChatApp.Api.DTOs;
using ChatApp.Api.Extensions;
using ChatApp.Api.Realtime;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConversationsFeature = ChatApp.Application.Features.Conversations;
using MessagesFeature = ChatApp.Application.Features.Messages;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(ISender sender, DetachedMemoryUpdateDispatcher memoryDispatcher) : ControllerBase
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

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Send.Command(id, request.Content), cancellationToken);
        if (result.IsSuccess)
        {
            memoryDispatcher.FireAndForget(id, request.Content);
        }
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] Guid? before, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Get.Query(id, before, limit), cancellationToken);
        return result.ToActionResult();
    }
    [HttpGet("{id:guid}/messages/search")]
    public async Task<IActionResult> SearchMessages(Guid id, [FromQuery] string q, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Search.Query(id, q, limit), cancellationToken);
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
}
