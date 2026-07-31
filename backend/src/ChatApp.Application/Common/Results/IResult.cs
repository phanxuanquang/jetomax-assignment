namespace ChatApp.Application.Common.Results;

/// <summary>
/// Lets pipeline behaviors (e.g. <see cref="Behaviors.ValidationBehavior{TRequest,TResponse}"/>)
/// construct a failure response without knowing whether <typeparamref name="TSelf"/> is the
/// non-generic <see cref="Result"/> or a specific <see cref="Result{T}"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete result type implementing this interface.</typeparam>
public interface IResult<TSelf> where TSelf : IResult<TSelf>
{
    /// <summary>Creates a failed <typeparamref name="TSelf"/> carrying <paramref name="error"/>.</summary>
    static abstract TSelf Failure(Error error);
}
