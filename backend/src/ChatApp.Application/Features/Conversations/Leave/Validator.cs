using FluentValidation;

namespace ChatApp.Application.Features.Conversations.Leave;

/// <summary>Validates the shape of <see cref="Command"/>: a valid id, and a defined enum value if <c>Mode</c> is given.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.Mode!.Value)
            .IsInEnum()
            .When(x => x.Mode.HasValue);
    }
}
