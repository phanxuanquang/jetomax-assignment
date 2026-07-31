using ChatApp.Api.Auth;
using ChatApp.Api.DTOs;
using ChatApp.Api.Extensions;
using ChatApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetAllConversationsFeature = ChatApp.Application.Features.Internal.GetAllConversations;
using PublishDigestFeature = ChatApp.Application.Features.Internal.PublishDigest;
using SetUserRoleFeature = ChatApp.Application.Features.Internal.SetUserRole;
using SummarizeConversationsFeature = ChatApp.Application.Features.Internal.SummarizeConversations;

namespace ChatApp.Api.Controllers;

/// <summary>
/// "Internal" is just a routing prefix, not an access boundary — access is entirely decided by
/// <see cref="AllowedRolesAttribute"/> per action. Controller default is Administrator-only; read/digest
/// actions widen to Administrator+Moderator.
/// </summary>
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
        var result = await sender.Send(new GetAllConversationsFeature.Query(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("summaries")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> SummarizeConversations([FromQuery] double hoursAgo = 24, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SummarizeConversationsFeature.Query(hoursAgo), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("digest")]
    [AllowedRoles(UserRole.Administrator, UserRole.Moderator)]
    public async Task<IActionResult> PublishDigest([FromBody] PublishDigestRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new PublishDigestFeature.Command(request.Digest, request.PublishedAt), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Administrator-only via the controller default: promoting/demoting a role always requires an existing Administrator, never self-service.</summary>
    [HttpPost("roles")]
    public async Task<IActionResult> SetUserRole([FromBody] SetUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SetUserRoleFeature.Command(request.Usernames, request.Role), cancellationToken);
        return result.ToActionResult();
    }
}
