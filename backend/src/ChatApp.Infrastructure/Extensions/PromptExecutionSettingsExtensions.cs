using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatApp.Infrastructure.Extensions;

internal static class PromptExecutionSettingsExtensions
{
    internal static GeminiPromptExecutionSettings Normalize<T>(this PromptExecutionSettings promptExecutionSettings, double temp)
    {
        if (promptExecutionSettings is GeminiPromptExecutionSettings settings)
        {
            if (typeof(T) != typeof(string))
            {
                settings.ResponseMimeType = "application/json";
                settings.ResponseSchema = typeof(T);
            }

            settings.Temperature = temp;
            return settings;
        }

        throw new NotImplementedException();
    }
}