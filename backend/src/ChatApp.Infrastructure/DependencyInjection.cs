using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Ai;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Infrastructure;

/// <summary>Registers every adapter this project implements: <see cref="IAppDbContext"/>, <see cref="IGenerativeAiService"/>, <see cref="IStorageClient"/>.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // §11: this connection string MUST use a role that bypasses Row-Level Security (Supabase's
        // service role) — an RLS-subject role makes auth.uid() NULL for this session, so every
        // policy evaluates false and every query silently returns zero rows rather than failing
        // loudly. See ConnectionStrings:Postgres.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddScoped<IGenerativeAiService, GeminiGenerativeAiService>();

        services.Configure<SupabaseStorageOptions>(configuration.GetSection("Supabase"));
        services.AddHttpClient<IStorageClient, SupabaseStorageClient>();

        return services;
    }
}
