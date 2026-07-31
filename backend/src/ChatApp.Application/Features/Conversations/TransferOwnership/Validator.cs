using FluentValidation;

namespace ChatApp.Application.Features.Conversations.TransferOwnership;

/// <summary>Validates the shape of <see cref="Command"/>: a valid conversation id and a well-formed username.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.NewOwnerUsername)
            .NotEmpty()
            .Matches("^[A-Za-z0-9]{1,30}$").WithMessage("NewOwnerUsername must be 1-30 letters/digits.");
    }
}
