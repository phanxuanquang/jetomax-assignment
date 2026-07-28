namespace ChatApp.Infrastructure.Ai;

/// <summary>Binds the <c>Gemini</c> configuration section (see <c>prerequisite-setups.md</c>).</summary>
public sealed class GeminiOptions
{
    /// <summary>Google AI Studio API key.</summary>
    public required string ApiKey { get; init; }

    /// <summary>Model id, e.g. <c>gemini-2.5-flash</c>.</summary>
    public required string Model { get; init; }
}
