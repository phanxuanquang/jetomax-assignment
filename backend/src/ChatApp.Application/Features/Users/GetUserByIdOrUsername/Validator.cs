using FluentValidation;

namespace ChatApp.Application.Features.Users.GetUserByIdOrUsername;

public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(x => x.IdOrUsername).NotEmpty();
    }
}
