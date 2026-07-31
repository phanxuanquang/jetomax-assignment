using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>Joins the caller into the conversation identified by <paramref name="PublicId"/>; rejected if frozen or deleted, a no-op if already joined.</summary>
/// <param name="PublicId">The exact, case-sensitive public code of the conversation to join.</param>
public sealed record Command(string PublicId) : IRequest<Result>;
