namespace ChatApp.Application.Common.Results;

/// <summary>
/// The outcome of a use case that produces a <typeparamref name="T"/> value on success, or a
/// failure carrying an <see cref="Error"/>. Deliberately does not inherit <see cref="Result"/>: a
/// method declared to return <see cref="Result"/> must not be able to accept a <see cref="Result{T}"/>
/// by upcast, and a <see cref="Result{T}"/> producer must not be able to silently resolve to the
/// non-generic <see cref="Result.Success()"/>/<see cref="Result.Failure"/> through inheritance.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public sealed class Result<T> : IResult<Result<T>>
{
    /// <summary>True when the use case succeeded; when false, <see cref="Error"/> is set.</summary>
    public bool IsSuccess { get; }

    /// <summary>The failure reason; null when <see cref="IsSuccess"/> is true.</summary>
    public Error? Error { get; }

    /// <summary>The produced value; default when the result is a failure.</summary>
    public T? Value { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Error = null;
        Value = value;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
        Value = default;
    }

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    public static Result<T> Failure(Error error) => new(error);
}
