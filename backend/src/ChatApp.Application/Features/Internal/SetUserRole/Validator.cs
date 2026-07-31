using FluentValidation;

namespace ChatApp.Application.Features.Internal.SetUserRole;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Usernames)
            .NotEmpty().WithMessage("At least one username is required.");

        RuleForEach(x => x.Usernames)
            .Matches("^[A-Za-z0-9]{1,30}$").WithMessage("Usernames must be 1-30 letters/digits.");

        RuleFor(x => x.Usernames)
            .Must(names => names.Distinct(StringComparer.Ordinal).Count() == names.Count)
            .WithMessage("Usernames must not contain duplicates.")
            .When(x => x.Usernames.Count > 0);

        RuleFor(x => x.Role).IsInEnum();
    }
}
