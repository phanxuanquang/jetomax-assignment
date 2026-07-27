using FluentValidation;

namespace ChatApp.Application.Features.Messages.Send;

/// <summary>Validates the shape of <see cref="Command"/>: a valid id and non-empty content.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
