namespace ChatApp.Application.Common.Results;

/// <summary>
/// The outcome of a use case that produces a <typeparamref name="T"/> value on success, or a
/// failure carrying an <see cref="Error"/>.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public sealed class Result<T> : Result, IResult<Result<T>>
{
    /// <summary>The produced value; default when the result is a failure.</summary>
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;

    private Result(Error error) : base(false, error) => Value = default;

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    public static new Result<T> Failure(Error error) => new(error);

    /// <summary>Wraps a value as a successful result, for concise <c>return</c> statements.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
