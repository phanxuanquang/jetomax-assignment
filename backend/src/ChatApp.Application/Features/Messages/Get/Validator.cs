using FluentValidation;

namespace ChatApp.Application.Features.Messages.Get;

/// <summary>Validates the shape of <see cref="Query"/>: a valid id and an in-range page size.</summary>
public sealed class Validator : AbstractValidator<Query>
{
    /// <summary>Defines the validation rules for <see cref="Query"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
