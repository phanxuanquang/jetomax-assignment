using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto Postgres. Exposes composable <see cref="IQueryable{T}"/> sources plus a small async
/// executor so handlers can build LINQ queries and run them asynchronously without Application
/// referencing EF Core (Application depends only on Domain, a mediator package, and FluentValidation).
/// Infrastructure implements this over EF Core, running each executor method through the real
/// async EF Core APIs.
/// </summary>
public interface IAppDbContext
{
    /// <summary>All users, including the hidden AI Agent.</summary>
    IQueryable<User> Users { get; }

    /// <summary>All conversations, including soft-deleted ones — handlers must filter <see cref="Conversation.IsDeleted"/> themselves.</summary>
    IQueryable<Conversation> Conversations { get; }

    /// <summary>All conversation memberships.</summary>
    IQueryable<Participant> Participants { get; }

    /// <summary>All messages, both <see cref="TextMessage"/> and <see cref="ImageMessage"/>; use <c>OfType&lt;T&gt;</c> to narrow.</summary>
    IQueryable<Message> Messages { get; }

    /// <summary>The 1:1 rolling memory state, one row per conversation.</summary>
    IQueryable<ConversationMemory> ConversationMemories { get; }

    /// <summary>The append-only chunk summary history, ordered by <see cref="ChunkMemory.Id"/>.</summary>
    IQueryable<ChunkMemory> ChunkMemories { get; }

    /// <summary>Marks <paramref name="entity"/> for insertion on the next <see cref="SaveChangesAsync"/>.</summary>
    void Add<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Marks each of <paramref name="entities"/> for insertion on the next <see cref="SaveChangesAsync"/>.</summary>
    void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    /// <summary>Marks <paramref name="entity"/> for deletion on the next <see cref="SaveChangesAsync"/>.</summary>
    void Remove<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Marks each of <paramref name="entities"/> for deletion on the next <see cref="SaveChangesAsync"/>.</summary>
    void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    /// <summary>Asynchronously executes <paramref name="query"/> and returns its first result, or null if empty.</summary>
    Task<TEntity?> FirstOrDefaultAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously executes <paramref name="query"/> and materializes every result.</summary>
    Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously executes <paramref name="query"/> and returns how many results it has.</summary>
    Task<int> CountAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>Asynchronously executes <paramref name="query"/> and returns whether it has any results.</summary>
    Task<bool> AnyAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// A case-insensitive substring match usable inside a LINQ predicate passed to one of this
    /// context's <see cref="IQueryable{T}"/> sources (e.g. <c>db.Conversations.Where(c =&gt;
    /// db.ILike(c.DisplayName, term))</c>). Infrastructure translates this to a DB-native operator
    /// (e.g. Postgres's <c>ILIKE</c> via <c>EF.Functions.ILike</c>) so the filter runs in SQL rather
    /// than pulling every row into memory — <c>string.Contains(text, StringComparison...)</c> does
    /// not translate and must not be used here.
    /// </summary>
    bool ILike(string value, string pattern);

    /// <summary>Persists every pending add/remove and property change, returning the number of affected rows.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
