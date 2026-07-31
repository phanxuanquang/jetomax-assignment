using ChatApp.Application.Common.Results;
using FluentValidation;
using MediatR;

namespace ChatApp.Application.Common.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for <typeparamref name="TRequest"/> before the
/// handler executes. If any validator reports a failure, the pipeline short-circuits and returns a
/// <see cref="ErrorType.Validation"/> failure instead of invoking the handler — this is the only
/// place format/shape validation happens; stateful business rules stay in the handler.
/// </summary>
/// <typeparam name="TRequest">The command or query being validated.</typeparam>
/// <typeparam name="TResponse">The handler's response type; must be a <see cref="Result"/> or <see cref="Result{T}"/>.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    /// <summary>
    /// Validates <paramref name="request"/> against every registered validator and either
    /// short-circuits with a validation failure or forwards to <paramref name="next"/>.
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        var message = string.Join(" ", failures.Select(f => f.ErrorMessage));
        return TResponse.Failure(Error.Validation("validation.failed", message));
    }
}
