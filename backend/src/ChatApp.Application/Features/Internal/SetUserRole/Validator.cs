using FluentValidation;

namespace ChatApp.Application.Features.Internal.SetSystemRoleForUsers;

public sealed class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.TargetUserIds)
            .NotEmpty().WithMessage("At least one user id is required.");
    }
}
