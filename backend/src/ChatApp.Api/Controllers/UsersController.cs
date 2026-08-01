using ChatApp.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersFeature = ChatApp.Application.Features.Users;

namespace ChatApp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetSigninUserMeta(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UsersFeature.GetSigninUserMeta.Query(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{idOrUsername}")]
    public async Task<IActionResult> GetByIdOrUsername(string idOrUsername, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UsersFeature.GetUserByIdOrUsername.Query(idOrUsername), cancellationToken);
        return result.ToActionResult();
    }
}
