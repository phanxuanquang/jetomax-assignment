using FluentValidation;

namespace ChatApp.Application.Features.Conversations.Rename;

/// <summary>
/// Validates the shape of <see cref="Command"/>: letters, digits, commas, and spaces only (§4.3's
/// cosmetic-text charset), up to 100 characters (not specified in the docs; a sane cap chosen here
/// — flag if a different limit is required).
/// </summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9, ]+$").WithMessage("DisplayName may only contain letters, digits, commas, and spaces.");
    }
}
