using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversations;

/// <summary>
/// Handles <see cref="Query"/>. Produces n8n's one 24-hour roll-up digest (§6.2) by reading each
/// active conversation's own on-demand summary via <see cref="ConversationMemoryService"/> directly
/// — never through <c>IMediator</c> — then folding all of them into a single overall digest. A pure
/// read, like the per-thread summary it reuses: nothing here mutates stored memory.
/// </summary>
public sealed class Handler(IAppDbContext db, ConversationMemoryService memoryService, IGenerativeAiService generativeAiService)
    : IRequestHandler<Query, Result<string>>
{
    /// <summary>Summarizes every conversation active within the last <see cref="Query.HoursAgo"/> hours and folds them into one digest.</summary>
    public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-request.HoursAgo);

        var conversations = await db.ToListAsync(
            db.Conversations
                .Where(c => c.Messages.Any(m => m.SentAt >= since))
                .OrderByDescending(c => c.LastMessageTime)
                .Select(c => new { c.Id, c.DisplayName }),
            cancellationToken);

        if (conversations.Count == 0)
        {
            return Result<string>.Failure(Error.NotFound("conversation.not_found", $"No conversations active within the last {request.HoursAgo} hours."));
        }

        var threadSummaries = new List<(string DisplayName, string Summary)>();
        foreach (var conversation in conversations)
        {
            var summary = await memoryService.GetOnDemandSummaryAsync(conversation.Id, cancellationToken);
            threadSummaries.Add((conversation.DisplayName, summary));
        }

        var digest = await generativeAiService.GenerateContentAsync<string>(
            ComposeDigestPrompt(request.HoursAgo, threadSummaries),
            cancellationToken: cancellationToken);

        return Result<string>.Success(digest);
    }

    private static string ComposeDigestPrompt(double hoursAgo, IReadOnlyList<(string DisplayName, string Summary)> threadSummaries)
    {
        var threads = string.Join("\n\n", threadSummaries.Select(t => $"### {t.DisplayName}\n{t.Summary}"));
        return $"""
            Produce one overall summary of chat activity across all conversations active in the
            last {hoursAgo} hours, based on each conversation's own summary below. Group related
            activity, keep it concise, and preserve concrete facts: names, decisions, numbers, and
            negations (things explicitly ruled out or denied).

            {threads}
            """;
    }
}
