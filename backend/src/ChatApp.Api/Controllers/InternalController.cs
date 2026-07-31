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
/// Bulk/administrative endpoints (§9.2). <c>Internal</c> here is just a routing prefix, not an access
/// boundary (decision B-1) — access is entirely decided by <see cref="AllowedRolesAttribute"/> per
/// action. Controller default is Administrator-only (the narrowest requirement in this group, backing
/// <see cref="SetUserRole"/>); the read/digest actions widen that to Administrator+Moderator via an
/// action-level override.
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

    /// <summary>
    /// Sets the system-wide role for one or more existing users, by username (F-1a). Administrator-only
    /// (the controller default — no action-level override needed here): the only way to promote/demote
    /// a role is an existing Administrator calling this, never self-service.
    /// </summary>
    [HttpPost("roles")]
    public async Task<IActionResult> SetUserRole([FromBody] SetUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SetUserRoleFeature.Command(request.Usernames, request.Role), cancellationToken);
        return result.ToActionResult();
    }
}
