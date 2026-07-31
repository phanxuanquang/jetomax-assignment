using FluentValidation;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>Validates the shape of <see cref="Command"/>: distinct, non-empty, well-formed other-participant usernames.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ParticipantUsernames)
            .NotEmpty().WithMessage("A conversation needs at least one other participant.");

        RuleForEach(x => x.ParticipantUsernames)
            .Matches("^[A-Za-z0-9]{1,30}$").WithMessage("Usernames must be 1-30 letters/digits.");

        RuleFor(x => x.ParticipantUsernames)
            .Must(names => names.Distinct(StringComparer.Ordinal).Count() == names.Count)
            .WithMessage("Participant usernames must not contain duplicates.")
            .When(x => x.ParticipantUsernames.Count > 0);
    }
}
