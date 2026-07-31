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
    [HttpGet("{idOrUsername}")]
    public async Task<IActionResult> GetByIdOrUsername(string idOrUsername, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new UsersFeature.GetUserByIdOrUsername.Query(idOrUsername), cancellationToken);
        return result.ToActionResult();
    }
}
