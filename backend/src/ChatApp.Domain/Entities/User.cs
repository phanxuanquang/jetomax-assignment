using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

/// <summary>
/// A registered human. Backed by the <c>profiles</c> table; <see cref="Id"/> equals the
/// corresponding Supabase <c>auth.users</c> id.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Unique identifier; equals the Supabase auth user id. Unlike sibling entities, this must be
    /// settable (not self-generated) since it has to match an id that already exists in Supabase's
    /// own auth.users table.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Unique login handle; the user's public identity. Format and uniqueness are enforced by
    /// FluentValidation (Application) and the DB CHECK/UNIQUE constraint (Infrastructure), not Domain.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// System-wide permission tier (default <see cref="UserRole.User"/>), distinct from per-conversation
    /// Owner/Member. Physically stored in the companion <c>user_roles</c> table (1:1 via shared key);
    /// mapped onto this same entity via EF Core entity splitting (Infrastructure's
    /// <c>UserConfiguration</c>) rather than a separate Domain type, so <c>User</c> stays one pure-model
    /// aggregate. Gated by <c>ChatApp.Api</c>'s <c>[AllowedRoles]</c> attribute, not Domain.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// When the user was created. Set automatically when the instance is constructed.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// Conversations currently owned by this user. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Conversation> OwnedConversations { get; set; } = [];

    /// <summary>
    /// Conversations this user participates in. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Participant> Participations { get; set; } = [];

    /// <summary>
    /// Messages sent by this user. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Message> SentMessages { get; set; } = [];
}
