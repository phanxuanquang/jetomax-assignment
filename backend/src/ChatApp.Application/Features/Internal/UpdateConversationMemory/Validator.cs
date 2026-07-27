using FluentValidation;

namespace ChatApp.Application.Features.Internal.UpdateConversationMemory;

public sealed class Validator : AbstractValidator<Query>
{
    /// <summary>Defines the validation rules for <see cref="Query"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.FromMessageId).NotEmpty();
    }
}