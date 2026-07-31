using FluentValidation;

namespace ChatApp.Application.Features.Messages.SendImage;

/// <summary>Validates the shape of <see cref="Command"/>: a valid id and a non-empty image URL.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ImageUrl).NotEmpty();
    }
}
