using ChatApp.Domain.Enums;

namespace ChatApp.Api.DTOs;

public sealed record PublishDigestRequest(string Digest, DateTime PublishedAt);

public sealed record SetUserRoleRequest(IReadOnlyCollection<string> Usernames, UserRole Role);
