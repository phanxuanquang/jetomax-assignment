using ChatApp.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers;

/// <summary>Maps an Application <see cref="Result"/>/<see cref="Result{T}"/> to the matching HTTP outcome (§4.3).</summary>
internal static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToErrorResult(result.Error!);

    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToErrorResult(result.Error!);

    private static IActionResult ToErrorResult(Error error)
    {
        var body = new { code = error.Code, message = error.Message };

        return error.Type switch
        {
            ErrorType.Validation => new BadRequestObjectResult(body),
            ErrorType.NotFound => new NotFoundObjectResult(body),
            ErrorType.Forbidden => new ObjectResult(body) { StatusCode = StatusCodes.Status403Forbidden },
            ErrorType.Conflict => new ConflictObjectResult(body),
            ErrorType.Unexpected => new ObjectResult(body) { StatusCode = StatusCodes.Status500InternalServerError },
            _ => new ObjectResult(body) { StatusCode = StatusCodes.Status500InternalServerError }
        };
    }
}
