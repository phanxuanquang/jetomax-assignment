using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversations;

public sealed class Handler(IAppDbContext db, IGenerativeAiService generativeAiService, IMediator mediator)
    : IRequestHandler<Query, Result<string>>
{
    public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow.AddHours(request.HoursAgo);

        var conversationIds = await db.ToListAsync(
            db.Conversations.Where(c => c.Messages.Any(m => m.SentAt >= timestamp)).Select(c => c.Id),
            cancellationToken);

        if (conversationIds.Count == 0)
        {
            return Result<string>.Failure(Error.NotFound("conversation.not_found", $"No conversations active within the last {request.HoursAgo} hours."));
        }

        var forceUpdateTasks = new List<Task>();

        foreach (var conversationId in conversationIds)
        {
            forceUpdateTasks.Add(mediator.Send(new ForceUpdateConversationMemory.Query(conversationId), cancellationToken));
        }

        await Task.WhenAll(forceUpdateTasks);

        var chunkMemories = await db.ToListAsync(
            db.Conversations
                .Where(c => conversationIds.Contains(c.Id) && c.ChunkMemories.Any())
                .OrderBy(c => c.LastMessageTime)
                .Select(c => new
                {
                    Meta = new
                    {
                        c.DisplayName,
                        c.Memory!.GlobalMemory,
                    },
                    MemoryChunks = c.ChunkMemories.Select(c => c.Memory)
                }),
            cancellationToken);

        var summarization = await generativeAiService.GenerateContentAsync<string>("Will add the prompt here", cancellationToken: cancellationToken);

        return Result<string>.Success(summarization);
    }
}
