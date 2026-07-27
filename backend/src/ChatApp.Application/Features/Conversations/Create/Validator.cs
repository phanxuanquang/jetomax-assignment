using FluentValidation;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>Validates the shape of <see cref="Command"/>: distinct, non-empty other-participant ids.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ParticipantUserIds)
            .NotEmpty().WithMessage("A conversation needs at least one other participant.");

        RuleForEach(x => x.ParticipantUserIds)
            .NotEqual(Guid.Empty).WithMessage("Participant ids must not be empty.");

        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Participant ids must not contain duplicates.")
            .When(x => x.ParticipantUserIds.Count > 0);
    }
}
