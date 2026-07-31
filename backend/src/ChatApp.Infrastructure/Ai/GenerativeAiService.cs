using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Extensions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ChatApp.Infrastructure.Ai;

/// <summary>
/// Implements <see cref="IGenerativeAiService"/> over Semantic Kernel's <see cref="IChatCompletionService"/>;
/// a thin adapter that only executes prompts composed by Application callers and shapes the response as <c>T</c>.
/// </summary>
internal sealed class GenerativeAiService(IChatCompletionService chatCompletionService, IStorageClient storageClient) : IGenerativeAiService
{
    private readonly IChatCompletionService _chatCompletionService = chatCompletionService;
    private readonly IStorageClient _storageClient = storageClient;

    /// <summary>Counts characters as a stand-in for tokens, avoiding a remote call to the provider's real token-counting API.</summary>
    public async Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        => text.Length;

    public async Task<T> GenerateContentAsync<T>(string prompt, string? systemInstruction = null, double temp = 1.0, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        if (!string.IsNullOrEmpty(systemInstruction)) history.AddSystemMessage(systemInstruction);
        history.AddUserMessage(prompt);

        var response = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            PromptSettingsFactory.Create<T>(temp),
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
            PromptSettingsFactory.Create<T>(temp),
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
