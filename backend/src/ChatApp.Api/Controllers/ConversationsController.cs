using ChatApp.Api.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConversationsFeature = ChatApp.Application.Features.Conversations;
using MessagesFeature = ChatApp.Application.Features.Messages;

namespace ChatApp.Api.Controllers;

/// <summary>
/// Thin REST surface over the <c>Conversations</c>/<c>Messages</c> Application slices (§9.2). Every
/// action just resolves <see cref="ISender"/> and forwards to the matching command/query — no
/// business logic lives here.
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [AllowedClients(Client.App, Client.Mcp)]
    public async Task<IActionResult> Get([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.Get.Query(q), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.Create.Command(request.ParticipantUserIds), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("join")]
    [AllowedClients(Client.App, Client.Mcp)]
    public async Task<IActionResult> Join([FromBody] JoinConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.Join.Command(request.PublicId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/name")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.Rename.Command(id, request.DisplayName), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/readonly")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> SetReadonly(Guid id, [FromBody] SetReadonlyRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.SetReadonly.Command(id, request.IsReadonly), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/transfer")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> TransferOwnership(Guid id, [FromBody] TransferOwnershipRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.TransferOwnership.Command(id, request.NewOwnerUserId), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/messages")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> GetMessages(Guid id, [FromQuery] Guid? before, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new MessagesFeature.Get.Query(id, before, limit), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/participants")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> AddParticipants(Guid id, [FromBody] ParticipantsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.AddParticipants.Command(id, request.UserIds), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}/participants")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> RemoveParticipants(Guid id, [FromBody] ParticipantsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.RemoveParticipants.Command(id, request.UserIds), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/leave")]
    [AllowedClients(Client.App)]
    public async Task<IActionResult> Leave(Guid id, [FromBody] LeaveConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConversationsFeature.Leave.Command(id, request.Mode), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/summary")]
    [AllowedClients(Client.App, Client.Mcp, Client.N8n)]
    public async Task<IActionResult> Summarize(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ChatApp.Application.Features.Internal.SummarizeConversation.Query(id), cancellationToken);
        return result.ToActionResult();
    }
}
