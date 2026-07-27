namespace ChatApp.Application.Features.Internal;

/// <summary>A conversation as listed for n8n's daily digest — just enough to loop over each thread and request its summary.</summary>
/// <param name="Id">The conversation's id, to pass to <c>SummarizeThread</c>.</param>
/// <param name="DisplayName">The conversation's display name.</param>
public sealed record ThreadDto(Guid Id, string DisplayName);
