using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ChatApp.Infrastructure.Ai;

/// <summary>
/// Implements the single AI port (<see cref="IGenerativeAiService"/>, §8) over Google Gemini via
/// Semantic Kernel's Google connector. A thin adapter only: prompts are composed entirely by
/// Application callers, this class only executes them and shapes the response as <c>T</c>.
/// </summary>
internal sealed class GeminiGenerativeAiService(IChatCompletionService chatCompletionService, IStorageClient storageClient) : IGenerativeAiService
{
    private readonly IChatCompletionService _chatCompletionService = chatCompletionService;
    private readonly IStorageClient _storageClient = storageClient;

    /// <summary>
    /// Local mock (decision: character count, not a remote call) — counts <paramref name="text"/>'s
    /// total characters. Deliberately not Gemini's real <c>countTokens</c> API.
    /// </summary>
    public async Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        => text.Length;

    public async Task<T> GenerateContentAsync<T>(string prompt, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        if (!string.IsNullOrEmpty(systemInstruction)) history.AddSystemMessage(systemInstruction);
        history.AddUserMessage(prompt);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            new PromptExecutionSettings().Normalize<T>(temp),
            cancellationToken: cancellationToken);

        var responseContent = response.Content ?? string.Empty;

        return typeof(T) == typeof(string)
            ? (T)(object)responseContent
            : JsonSerializer.Deserialize<T>(responseContent)!;
    }

    public async Task<T> GenerateContentFromImageAsync<T>(string prompt, byte[] imageAsBytes, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        if (!string.IsNullOrEmpty(systemInstruction)) history.AddSystemMessage(systemInstruction);
        history.AddUserMessage(new ChatMessageContentItemCollection
        {
            new TextContent(prompt),
            new ImageContent(imageAsBytes, imageAsBytes.GetMimeType())
        });

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            new PromptExecutionSettings().Normalize<T>(temp),
            cancellationToken: cancellationToken);

        var responseContent = response.Content ?? string.Empty;

        return typeof(T) == typeof(string)
            ? (T)(object)responseContent
            : JsonSerializer.Deserialize<T>(responseContent)!;
    }

    public async Task<T> GenerateContentFromImageAsync<T>(string prompt, string imageUrl, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var bytes = await _storageClient.DownloadAsync(imageUrl, cancellationToken);
        return await GenerateContentFromImageAsync<T>(prompt, bytes, systemInstruction, temp, cancellationToken);
    }
}
