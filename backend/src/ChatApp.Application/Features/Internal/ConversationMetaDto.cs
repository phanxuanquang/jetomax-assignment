namespace ChatApp.Application.Features.Internal;

/// <summary>A conversation as listed for n8n's daily digest — just enough to loop over each thread and request its summary.</summary>
/// <param name="ConversationId">The conversation's id, to pass to <c>SummarizeThread</c>.</param>
/// <param name="DisplayName">The conversation's display name.</param>
public sealed record ConversationMetaDto(Guid ConversationId, string DisplayName);
