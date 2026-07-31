using FluentValidation;

namespace ChatApp.Application.Features.Internal.SummarizeConversations;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.HoursAgo).GreaterThan(0);
    }
}
