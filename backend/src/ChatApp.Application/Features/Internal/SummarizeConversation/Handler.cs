using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

public sealed class Handler(IAppDbContext db, IGenerativeAiService generativeAiService, IMediator mediator)
    : IRequestHandler<Query, Result<string>>
{
    public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        var conversationExists = await db.AnyAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId && c.Messages.Any()),
            cancellationToken);

        if (!conversationExists)
        {
            return Result<string>.Failure(Error.NotFound("conversation.not_found", "Conversation not found or does not has any messages."));
        }

        var result = await mediator.Send(new ForceUpdateConversationMemory.Query(request.ConversationId), cancellationToken);

        var summarization = await generativeAiService.GenerateContentAsync<string>("Will add the prompt here", cancellationToken: cancellationToken);

        return Result<string>.Success(summarization);
    }
}
