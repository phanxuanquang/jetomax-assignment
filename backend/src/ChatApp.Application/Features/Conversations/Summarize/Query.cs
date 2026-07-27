using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Summarize;

/// <summary>
/// Returns a conversation's thread summary (F-7): the pre-computed global memory plus a fresh
/// summary of messages since the last checkpoint. One query serves the in-app action, the MCP
/// <c>summarize_thread</c> tool, and the n8n daily digest.
/// </summary>
/// <param name="ConversationId">The conversation to summarize.</param>
public sealed record Query(Guid ConversationId) : IRequest<Result<ThreadSummaryDto>>;
