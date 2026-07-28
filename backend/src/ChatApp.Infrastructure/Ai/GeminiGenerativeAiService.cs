using System.Text.Json;
using ChatApp.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatApp.Infrastructure.Ai;

/// <summary>
/// Implements the single AI port (<see cref="IGenerativeAiService"/>, §8) over Google Gemini via
/// Semantic Kernel's Google connector. A thin adapter only: prompts are composed entirely by
/// Application callers, this class only executes them and shapes the response as <c>T</c>.
/// </summary>
public sealed class GeminiGenerativeAiService : IGenerativeAiService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IStorageClient _storageClient;

    public GeminiGenerativeAiService(IOptions<GeminiOptions> options, IStorageClient storageClient)
    {
        _storageClient = storageClient;

#pragma warning disable SKEXP0070
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: options.Value.Model, apiKey: options.Value.ApiKey);
        var kernel = kernelBuilder.Build();
#pragma warning restore SKEXP0070

        _chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <summary>
    /// Local mock (decision: character count, not a remote call) — counts <paramref name="text"/>'s
    /// total characters. Deliberately not Gemini's real <c>countTokens</c> API.
    /// </summary>
    public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default) =>
        Task.FromResult(text.Length);

    public async Task<T> GenerateContentAsync<T>(string prompt, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            history, BuildExecutionSettings<T>(temp), cancellationToken: cancellationToken);

        return Deserialize<T>(response.Content ?? string.Empty);
    }

    public async Task<T> GenerateContentFromImageAsync<T>(string prompt, byte[] imageAsBytes, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddUserMessage(new ChatMessageContentItemCollection
        {
            new TextContent(prompt),
            new ImageContent(imageAsBytes, DetectMimeType(imageAsBytes))
        });

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            history, BuildExecutionSettings<T>(temp), cancellationToken: cancellationToken);

        return Deserialize<T>(response.Content ?? string.Empty);
    }

    public async Task<T> GenerateContentFromImageAsync<T>(string prompt, string imageUrl, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var bytes = await _storageClient.DownloadAsync(imageUrl, cancellationToken);
        return await GenerateContentFromImageAsync<T>(prompt, bytes, temp, cancellationToken);
    }

    private static GeminiPromptExecutionSettings BuildExecutionSettings<T>(double temp)
    {
        var settings = new GeminiPromptExecutionSettings { Temperature = temp };

        if (typeof(T) != typeof(string))
        {
            settings.ResponseMimeType = "application/json";
            settings.ResponseSchema = typeof(T);
        }

        return settings;
    }

    private static T Deserialize<T>(string content)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)content;
        }

        return JsonSerializer.Deserialize<T>(content)
            ?? throw new InvalidOperationException("Gemini returned no content to deserialize.");
    }

    /// <summary>Sniffs the image format from its magic bytes — <see cref="IStorageClient"/> only returns raw bytes, no content-type metadata.</summary>
    private static string DetectMimeType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return "image/png";
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
        {
            return "image/gif";
        }

        if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }
}
