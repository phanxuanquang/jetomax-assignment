using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Internal.UpdateConversationMemory;

public sealed class Handler(IAppDbContext db, IGenerativeAiService generativeAiService, IMediator mediator)
    : IRequestHandler<Query, Result<ConversationMemoryDto>>
{
    public async Task<Result<ConversationMemoryDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var conversationExists = await db.AnyAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId),
            cancellationToken);

        if (!conversationExists)
        {
            return Result<ConversationMemoryDto>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        var message = await db.FirstOrDefaultAsync(
            db.Messages.Where(m => m.Id == request.FromMessageId && m.ConversationId == request.ConversationId),
            cancellationToken);

        if (message == null)
        {
            return Result<ConversationMemoryDto>.Failure(Error.NotFound("message.not_found", "Message not found."));
        }

        var messagesSinceFromMessage = await db.ToListAsync(
            db.Messages.Where(m => m.ConversationId == request.ConversationId && m.SentAt >= message!.SentAt).OrderBy(m => m.SentAt),
            cancellationToken);

        var messageContents = messagesSinceFromMessage
            .Select(m =>
            {
                if (m is TextMessage textMessage)
                    return textMessage.Content;

                if (m is ImageMessage imageMessage)
                    return imageMessage.Caption;

                return string.Empty;
            })
            .Where(t => !string.IsNullOrEmpty(t));

        var totalTokens = await generativeAiService.CountTokensAsync(string.Join("\n", messageContents));

        if (totalTokens >= 5000) // TODO: Set at configurable in the appsettings.json
        {
            var result = await mediator.Send(new ForceUpdateConversationMemory.Query(request.ConversationId), cancellationToken);
            return result;
        }

        var currentConversationMemory = await db.FirstOrDefaultAsync(
           db.ConversationMemories.Where(m => m.ConversationId == request.ConversationId).Select(m => m.GlobalMemory),
           cancellationToken);

        return Result<ConversationMemoryDto>.Success(new ConversationMemoryDto(currentConversationMemory!, "Nothing to update"));
    }
}