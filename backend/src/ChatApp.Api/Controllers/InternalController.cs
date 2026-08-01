using ChatApp.Api.Auth;
using ChatApp.Api.DTOs;
using ChatApp.Api.Extensions;
using ChatApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InternalFeatures = ChatApp.Application.Features.Internal;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/internal")]
[Authorize]
[AllowedRoles(UserRole.Administrator)]
public sealed class InternalController(ISender sender) : ControllerBase
{
    [HttpGet("conversations")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> GetAllConversations(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new InternalFeatures.GetAllConversations.Query(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/summary")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> SummarizeConversation(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new InternalFeatures.SummarizeConversation.Query(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("summaries")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> SummarizeConversations([FromQuery] double hoursAgo = 24, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new InternalFeatures.SummarizeConversations.Query(hoursAgo), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("digest")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> PublishDigest([FromBody] PublishDigestRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new InternalFeatures.PublishDigest.Command(request.Digest, request.PublishedAt), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("roles")]
    [AllowedRoles(UserRole.Administrator)]
    public async Task<IActionResult> SetUserRole([FromBody] SetUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new InternalFeatures.SetUserRole.Command(request.Usernames, request.Role), cancellationToken);
        return result.ToActionResult();
    }
}
