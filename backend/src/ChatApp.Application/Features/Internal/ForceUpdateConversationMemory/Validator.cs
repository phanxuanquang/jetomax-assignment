using FluentValidation;

namespace ChatApp.Application.Features.Internal.ForceUpdateConversationMemory;

public sealed class Validator : AbstractValidator<Query>
{
    /// <summary>Defines the validation rules for <see cref="Query"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}