using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Ai;
using ChatApp.Infrastructure.Options;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ChatApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<GeminiConnectionOptions>()
                .Bind(configuration.GetSection(nameof(GeminiConnectionOptions)))
                .ValidateOnStart();
        var geminiOptions = configuration
            .GetSection(nameof(GeminiConnectionOptions))
            .Get<GeminiConnectionOptions>()
                ?? throw new InvalidOperationException($"Failed to bind {nameof(GeminiConnectionOptions)} from configuration.");
        services.AddGoogleAIGeminiChatCompletion(modelId: geminiOptions.ModelId ?? "gemini-flash-lite-latest", apiKey: geminiOptions.ApiKey);

        services.AddOptions<SupabaseStorageOptions>()
                .Bind(configuration.GetSection(nameof(SupabaseStorageOptions)))
                .ValidateOnStart();
        services.AddHttpClient<IStorageClient, SupabaseStorageClient>();

        services.AddScoped<IGenerativeAiService, GeminiGenerativeAiService>();
        return services;
    }
}
