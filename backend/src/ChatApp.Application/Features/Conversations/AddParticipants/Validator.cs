using FluentValidation;

namespace ChatApp.Application.Features.Conversations.AddParticipants;

/// <summary>Validates the shape of <see cref="Command"/>: a valid conversation id and at least one distinct, non-empty user id.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user id is required.");

        RuleForEach(x => x.UserIds)
            .NotEqual(Guid.Empty).WithMessage("User ids must not be empty.");

        RuleFor(x => x.UserIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("User ids must not contain duplicates.")
            .When(x => x.UserIds.Count > 0);
    }
}
