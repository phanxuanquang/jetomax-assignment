namespace ChatApp.Api.Controllers;

public sealed record PublishDigestRequest(string Digest, DateTime PublishedAt);
