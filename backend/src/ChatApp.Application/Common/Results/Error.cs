namespace ChatApp.Application.Common.Results;

/// <summary>
/// Describes why a use case did not succeed: a stable <paramref name="Code"/> for callers to branch
/// on, a human-readable <paramref name="Message"/>, and an <see cref="ErrorType"/> for transport mapping.
/// </summary>
/// <param name="Code">A short, stable identifier for this failure (e.g. <c>"conversation.frozen"</c>).</param>
/// <param name="Message">A human-readable explanation, safe to surface to the caller.</param>
/// <param name="Type">The category of failure, used to map to a transport-level outcome.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Forbidden"/> error.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>Creates a <see cref="ErrorType.Unexpected"/> error.</summary>
    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
