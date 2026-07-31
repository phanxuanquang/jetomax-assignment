using FluentValidation;

namespace ChatApp.Application.Features.Conversations.SetReadonly;

/// <summary>Validates the shape of <see cref="Command"/>: a valid id (<c>IsReadonly</c> is a plain bool, no format rule applies).</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
