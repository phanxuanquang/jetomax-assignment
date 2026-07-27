using FluentValidation;

namespace ChatApp.Application.Features.Messages.OcrImageMessage;

/// <summary>Validates the shape of <see cref="Command"/>: a valid message id.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}
