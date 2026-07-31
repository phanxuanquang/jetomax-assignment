using FluentValidation;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
