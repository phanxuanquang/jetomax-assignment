namespace ChatApp.Infrastructure.Options;

public sealed class GeminiConnectionOptions
{
    public required string ApiKey { get; init; }

    public required string ModelId { get; init; }
}
