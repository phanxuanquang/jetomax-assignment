using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatApp.Infrastructure.Extensions;

/// <summary>
/// The one place that knows which concrete <see cref="PromptExecutionSettings"/> subtype the active AI
/// provider needs (currently Gemini's <see cref="GeminiPromptExecutionSettings"/>). Callers only ever
/// see the provider-agnostic base <see cref="PromptExecutionSettings"/> return type — swapping
/// <c>ChatApp.Infrastructure.Ai.GenerativeAiService</c>'s underlying provider (e.g. to OpenAI or
/// Claude) should only ever require changing <see cref="Create{T}"/>'s body (construct that provider's
/// settings type instead) plus the DI registration/connector package, never the caller.
/// </summary>
internal static class PromptSettingsFactory
{
    /// <summary>
    /// Builds the execution settings for a call expected to return <typeparamref name="T"/>: requests
    /// structured JSON output shaped like <typeparamref name="T"/> unless <typeparamref name="T"/> is
    /// <see cref="string"/> (free-form text), and applies <paramref name="temp"/>.
    /// </summary>
    internal static PromptExecutionSettings Create<T>(double temp)
    {
        var settings = new GeminiPromptExecutionSettings { Temperature = temp };

        if (typeof(T) != typeof(string))
        {
            settings.ResponseMimeType = "application/json";
            settings.ResponseSchema = typeof(T);
        }

        return settings;
    }
}
