namespace ChatApp.Application.Common.Results;

/// <summary>
/// Categorizes an <see cref="Error"/> so callers (controllers, the Hub, MCP tools) can map it to the
/// right transport-level outcome (e.g. HTTP 400/403/404/409) without inspecting its message text.
/// </summary>
public enum ErrorType
{
    /// <summary>Input failed FluentValidation's format/shape rules.</summary>
    Validation,

    /// <summary>The requested resource does not exist, or is invisible to the caller (e.g. not a participant).</summary>
    NotFound,

    /// <summary>The caller is authenticated but not allowed to perform this action (e.g. not the owner).</summary>
    Forbidden,

    /// <summary>The request conflicts with the current state of the resource (e.g. frozen, already processing).</summary>
    Conflict,

    /// <summary>An unanticipated failure that doesn't fit the other categories.</summary>
    Unexpected
}
