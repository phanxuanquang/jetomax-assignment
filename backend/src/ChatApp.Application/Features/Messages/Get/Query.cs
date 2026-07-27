using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Messages.Get;

/// <summary>
/// Returns a page of a conversation's message history, newest first (F-2). An omitted
/// <paramref name="Before"/> starts from the newest message; otherwise only messages strictly
/// older than that message are returned — pass the oldest id from the previous page to page further back.
/// </summary>
/// <param name="ConversationId">The conversation to read history from.</param>
/// <param name="Before">The id of a message already seen by the caller; results are strictly older than it.</param>
/// <param name="Limit">How many messages to return (1–100; defaults to 50).</param>
public sealed record Query(Guid ConversationId, Guid? Before, int Limit = 50) : IRequest<Result<IReadOnlyList<MessageDto>>>;
