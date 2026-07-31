using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Persistence.Conversions;
using ChatApp.Infrastructure.Persistence.Translation;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence;

/// <summary>
/// EF Core / Npgsql implementation of <see cref="IAppDbContext"/> over the Supabase Postgres database;
/// never creates or alters the schema, only maps to tables that already exist. Must connect using a role
/// that bypasses Row-Level Security (Supabase's service role) — a role subject to RLS makes <c>auth.uid()</c>
/// NULL here, so every policy evaluates false and queries silently return zero rows instead of failing loudly.
/// </summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public IQueryable<User> Users => Set<User>();

    public IQueryable<Conversation> Conversations => Set<Conversation>();

    public IQueryable<Participant> Participants => Set<Participant>();

    public IQueryable<Message> Messages => Set<Message>();

    public IQueryable<ConversationMemory> ConversationMemories => Set<ConversationMemory>();

    public IQueryable<ChunkMemory> ChunkMemories => Set<ChunkMemory>();

    public new void Add<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Add(entity);

    public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class => Set<TEntity>().AddRange(entities);

    public new void Remove<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Remove(entity);

    public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class => Set<TEntity>().RemoveRange(entities);

    public Task<TEntity?> FirstOrDefaultAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        Rewrite(query).FirstOrDefaultAsync(cancellationToken);

    public Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        Rewrite(query).ToListAsync(cancellationToken);

    public Task<int> CountAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        Rewrite(query).CountAsync(cancellationToken);

    public Task<bool> AnyAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default) =>
        Rewrite(query).AnyAsync(cancellationToken);

    /// <summary>
    /// Every executor method rewrites the query's expression tree first, swapping calls to
    /// <see cref="IAppDbContext.ILike"/> (the interface's own <c>MethodInfo</c>, which Npgsql's
    /// translator has never seen) for Npgsql's own <c>EF.Functions.ILike</c> call, which it does
    /// recognize. See <see cref="ILikeRewriter"/>.
    /// </summary>
    private static IQueryable<TEntity> Rewrite<TEntity>(IQueryable<TEntity> query) =>
        query.Provider.CreateQuery<TEntity>(ILikeRewriter.Rewrite(query.Expression));

    /// <summary>
    /// Runtime fallback only (e.g. direct, non-query invocation); real query usage never reaches
    /// this body — <see cref="Rewrite{TEntity}"/> replaces the call before the query executes.
    /// </summary>
    public bool ILike(string value, string pattern) => value.Contains(pattern, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Populates each newly-added <see cref="Message"/>'s shadow <c>type</c> column from its concrete
    /// CLR type before delegating to the base save — <see cref="Message.Type"/> is computed, not a
    /// settable scalar, so it cannot back the column directly (see <see cref="Configurations.MessageConfiguration"/>).
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Message>().Where(e => e.State == EntityState.Added))
        {
            entry.Property("type").CurrentValue = entry.Entity switch
            {
                TextMessage or ImageMessage => entry.Entity.Type.ToString().ToLower(),
                _ => throw new InvalidOperationException($"Unmapped message type: {entry.Entity.GetType().Name}")
            };
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Guarantee every DateTime read from/written to a timestamptz column is Kind=Utc, regardless
        // of Npgsql's own timestamp-kind defaults.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
