using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.PublishDigest;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IConversationNotifier notifier) : IRequestHandler<Command, Result>
{
    /// <summary>Relays the digest via <see cref="IConversationNotifier.NotifyDigestPublishedAsync"/>; nothing is persisted.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        await notifier.NotifyDigestPublishedAsync(request.Digest, request.PublishedAt, cancellationToken);
        return Result.Success();
    }
}
