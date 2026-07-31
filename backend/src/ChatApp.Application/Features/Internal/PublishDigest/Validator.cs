using FluentValidation;

namespace ChatApp.Application.Features.Internal.PublishDigest;

/// <summary>Validates the shape of <see cref="Command"/>: non-empty digest content.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.Digest).NotEmpty();
    }
}
