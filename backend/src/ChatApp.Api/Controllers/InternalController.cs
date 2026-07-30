using ChatApp.Api.Auth;
using ChatApp.Api.DTOs;
using ChatApp.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GetAllConversationsFeature = ChatApp.Application.Features.Internal.GetAllConversations;
using PublishDigestFeature = ChatApp.Application.Features.Internal.PublishDigest;
using SummarizeConversationsFeature = ChatApp.Application.Features.Internal.SummarizeConversations;

namespace ChatApp.Api.Controllers;

/// <summary>
/// N8n-only bulk endpoints (§9.2): all non-deleted conversations, the 24h roll-up digest, and
/// publishing that digest. <c>Internal</c> here is just a routing prefix, not an access boundary
/// (decision B-1) — access is entirely decided by <see cref="AllowedClientsAttribute"/> per action.
/// </summary>
[ApiController]
[Route("api/internal")]
[Authorize]
[AllowedClients(Client.N8n)]
public sealed class InternalController(ISender sender) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetAllConversations(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllConversationsFeature.Query(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("summaries")]
    public async Task<IActionResult> SummarizeConversations([FromQuery] double hoursAgo = 24, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SummarizeConversationsFeature.Query(hoursAgo), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("digest")]
    public async Task<IActionResult> PublishDigest([FromBody] PublishDigestRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishDigestFeature.Command(request.Digest, request.PublishedAt), cancellationToken);
        return result.ToActionResult();
    }
}
