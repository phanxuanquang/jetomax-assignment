using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Get;

/// <summary>Lists the caller's conversations, ordered by recency; an empty/null <see cref="SearchTerm"/> returns all of them, otherwise only those whose <c>DisplayName</c> contains it (case-insensitive).</summary>
/// <param name="SearchTerm">Optional free-text filter over <c>DisplayName</c>.</param>
public sealed record Query(string? SearchTerm = null) : IRequest<Result<IReadOnlyList<ConversationDto>>>;
