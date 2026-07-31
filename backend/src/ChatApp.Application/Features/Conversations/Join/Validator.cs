using FluentValidation;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>Validates the shape of <see cref="Command"/>: exactly 6 case-sensitive alphanumeric characters, matching the DB's <c>public_id</c> format.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty()
            .Matches("^[A-Za-z0-9]{6}$").WithMessage("PublicId must be exactly 6 letters/digits.");
    }
}
