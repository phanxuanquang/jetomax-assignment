using System.Collections.Concurrent;

namespace ChatApp.Api.Realtime;

/// <summary>
/// Tracks which SignalR connection ids belong to which user id, so <see cref="SignalRConversationNotifier"/>
/// can update an already-connected user's live sockets immediately on a membership change. In-memory
/// and single-instance only; a multi-instance deployment would need a shared backplane (e.g. Redis).
/// </summary>
public interface IUserConnectionTracker
{
    void Add(Guid userId, string connectionId);
    void Remove(Guid userId, string connectionId);
    IReadOnlyCollection<string> GetConnections(Guid userId);
}

/// <summary>Default in-process implementation of <see cref="IUserConnectionTracker"/>, registered as a singleton.</summary>
public sealed class UserConnectionTracker : IUserConnectionTracker
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _connectionsByUser = new();

    public void Add(Guid userId, string connectionId)
    {
        var connections = _connectionsByUser.GetOrAdd(userId, static _ => new ConcurrentDictionary<string, byte>());
        connections[connectionId] = 0;
    }

    public void Remove(Guid userId, string connectionId)
    {
        if (_connectionsByUser.TryGetValue(userId, out var connections))
        {
            connections.TryRemove(connectionId, out _);
        }
    }

    public IReadOnlyCollection<string> GetConnections(Guid userId) =>
        _connectionsByUser.TryGetValue(userId, out var connections) ? connections.Keys.ToList() : [];
}
