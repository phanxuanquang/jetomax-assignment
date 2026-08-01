using ChatApp.Api.Options;
using ChatApp.Application.Memory;
using Microsoft.Extensions.Options;

namespace ChatApp.Api.Realtime;

/// <summary>
/// Fires <see cref="ConversationMemoryService.RecordMessageAndProcessAsync"/> as an un-awaited,
/// own-scoped background task — never the caller's own request-scoped services, which may already be
/// disposed by the time the task runs. Shared by <see cref="ChatHub"/> and the REST send endpoint so a
/// message counts toward conversation memory regardless of which transport sent it.
/// </summary>
public sealed class DetachedMemoryUpdateDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<ConversationMemoryOptions> memoryOptions,
    ILogger<DetachedMemoryUpdateDispatcher> logger)
{
    /// <summary>Schedules the memory update for <paramref name="conversationId"/>; returns immediately without waiting for it.</summary>
    public void FireAndForget(Guid conversationId, string messageText)
    {
        var thresholdTokens = memoryOptions.Value.TokenThreshold;

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            try
            {
                var memoryService = scope.ServiceProvider.GetRequiredService<ConversationMemoryService>();
                await memoryService.RecordMessageAndProcessAsync(conversationId, messageText, thresholdTokens, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Detached memory update failed for conversation {ConversationId}", conversationId);
            }
        });
    }
}
