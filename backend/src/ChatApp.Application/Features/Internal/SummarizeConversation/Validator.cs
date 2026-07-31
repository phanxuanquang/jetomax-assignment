using FluentValidation;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

/// <summary>Validates the shape of <see cref="Query"/>: a valid id.</summary>
public sealed class Validator : AbstractValidator<Query>
{
    /// <summary>Defines the validation rules for <see cref="Query"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
