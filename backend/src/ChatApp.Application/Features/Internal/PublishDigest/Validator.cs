using FluentValidation;

namespace ChatApp.Application.Features.Internal.PublishDigest;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Digest).NotEmpty();
    }
}
