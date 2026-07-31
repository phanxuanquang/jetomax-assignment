using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.PublishDigest;

/// <summary>
/// Relays a 24-hour digest to whatever page/channel displays it. Stateless: the backend does not
/// persist the digest, only broadcasts it via <see cref="Abstractions.IConversationNotifier"/>.
/// </summary>
/// <param name="Digest">The digest content to relay.</param>
/// <param name="PublishedAt">When the digest was produced.</param>
public sealed record Command(string Digest, DateTime PublishedAt) : IRequest<Result>;
