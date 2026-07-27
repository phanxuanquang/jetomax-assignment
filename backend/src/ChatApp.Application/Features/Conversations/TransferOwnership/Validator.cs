using FluentValidation;

namespace ChatApp.Application.Features.Conversations.TransferOwnership;

/// <summary>Validates the shape of <see cref="Command"/>: both ids present.</summary>
public sealed class Validator : AbstractValidator<Command>
{
    /// <summary>Defines the validation rules for <see cref="Command"/>.</summary>
    public Validator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.NewOwnerUserId).NotEmpty();
    }
}
