namespace ChatApp.Application.Common.Results;

/// <summary>
/// The outcome of a use case that produces no value: either success, or a failure carrying an
/// <see cref="Error"/>. Handlers return this instead of throwing for expected, "business" failures
/// (not found, forbidden, conflict, validation).
/// </summary>
public class Result : IResult<Result>
{
    /// <summary>True when the use case succeeded; when false, <see cref="Error"/> is set.</summary>
    public bool IsSuccess { get; }

    /// <summary>The failure reason; null when <see cref="IsSuccess"/> is true.</summary>
    public Error? Error { get; }

    /// <summary>Constructs a result in either outcome; use <see cref="Success"/>/<see cref="Failure"/> instead.</summary>
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    public static Result Failure(Error error) => new(false, error);
}
