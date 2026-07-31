namespace ChatApp.Application.Abstractions;

/// <summary>
/// The single AI port: text generation, image-grounded generation, and token counting, backed by
/// Google Gemini via Semantic Kernel in Infrastructure. Callers in Application compose the prompt
/// string and the desired response shape <typeparamref name="T"/>; Infrastructure only executes the
/// call and produces a value of that shape — it does not hold or know about the prompt's content.
/// </summary>
public interface IGenerativeAiService
{
    /// <summary>
    /// Counts <paramref name="text"/>'s tokens for the configured model. A real, remote call —
    /// never run on the message-send response path; only from a detached scope.
    /// </summary>
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content from <paramref name="prompt"/> alone, returning it shaped as
    /// <typeparamref name="T"/> (typically <see cref="string"/>, or a structured type when the
    /// prompt asks for one). The caller owns the prompt contract that makes <typeparamref name="T"/> valid.
    /// </summary>
    Task<T> GenerateContentAsync<T>(string prompt, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content from <paramref name="prompt"/> grounded in the image at
    /// <paramref name="imageAsBytes"/>, returning it shaped as <typeparamref name="T"/>.
    /// </summary>
    Task<T> GenerateContentFromImageAsync<T>(string prompt, byte[] imageAsBytes, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates content from <paramref name="prompt"/> grounded in the image at
    /// <paramref name="imageUrl"/>, returning it shaped as <typeparamref name="T"/>.
    /// </summary>
    Task<T> GenerateContentFromImageAsync<T>(string prompt, string imageUrl, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default);
}
