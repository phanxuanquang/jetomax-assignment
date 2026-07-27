using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Internal.ForceUpdateConversationMemory;

public sealed class Handler(IAppDbContext db, IGenerativeAiService generativeAiService)
    : IRequestHandler<Query, Result<ConversationMemoryDto>>
{
    public async Task<Result<ConversationMemoryDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var conversationMeta = await db.FirstOrDefaultAsync(
             db.Conversations
                .Where(c => c.Id == request.ConversationId && c.Messages.Any())
                .Select(c => new
                {
                    c.Memory!.GlobalMemory,
                    LatestMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new
                        {
                            m.Id,
                            m.SentAt
                        })
                        .First(),
                    LatestChunkMemoryMeta = c.ChunkMemories
                        .OrderByDescending(m => m.CreatedTime)
                        .Select(m => new
                        {
                            Content = m.Memory,
                            AssociatedEndMessage = new
                            {
                                m.EndMessage!.Id,
                                m.EndMessage!.SentAt
                            }
                        })
                        .First(),

                }),
             cancellationToken);

        if (conversationMeta == null)
        {
            return Result<ConversationMemoryDto>.Failure(Error.NotFound("conversation.not_found", "Conversation not found or does not has any messages."));
        }

        var latestChunkMemoryEndMessage = conversationMeta.LatestChunkMemoryMeta!.AssociatedEndMessage!;
        var latestMessage = conversationMeta.LatestMessage!;

        if (latestMessage.Id == latestChunkMemoryEndMessage.Id)
        {
            return Result<ConversationMemoryDto>.Success(new ConversationMemoryDto(conversationMeta!.GlobalMemory, conversationMeta!.LatestChunkMemoryMeta.Content));
        }

        var messagesToCreateChunkMemory = await db.ToListAsync(
            db.Messages
                .Where(m => m.ConversationId == request.ConversationId
                    && (m.SentAt >= latestChunkMemoryEndMessage.SentAt && m.Id != latestChunkMemoryEndMessage.Id)
                    && m.SentAt <= latestMessage.SentAt)
                .OrderBy(m => m.SentAt),
            cancellationToken);

        var newMemoryChunkContent = await generativeAiService.GenerateContentAsync<string>("Will add the prompt here", cancellationToken: cancellationToken);

        db.Add(new ChunkMemory
        {
            ConversationId = request.ConversationId,
            StartMessageId = messagesToCreateChunkMemory.First().Id,
            EndMessageId = messagesToCreateChunkMemory.Last().Id,
            Memory = newMemoryChunkContent
        });

        var updatedGlobalMemoryContent = await generativeAiService.GenerateContentAsync<string>("Will add the prompt here", cancellationToken: cancellationToken);

        var currentConversationMemory = await db.FirstOrDefaultAsync(
             db.Conversations.Where(c => c.Id == request.ConversationId).Select(c => c.Memory),
             cancellationToken);

        currentConversationMemory!.GlobalMemory = updatedGlobalMemoryContent;
        currentConversationMemory!.AssociatedEndMessageId = latestMessage.Id;
        currentConversationMemory!.LastUpdatedTime = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Result<ConversationMemoryDto>.Success(new ConversationMemoryDto(updatedGlobalMemoryContent, newMemoryChunkContent));
    }
}