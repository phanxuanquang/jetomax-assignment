namespace ChatApp.Domain.Enums;

/// <summary>
/// System-wide permission tier, one per user (see the <c>user_roles</c> table). Distinct from
/// per-conversation Owner/Member — this gates what an authenticated request may do, decided by
/// <c>ChatApp.Api</c>'s <c>[AllowedRoles]</c> attribute (an Api-layer concern). The type itself lives
/// in Domain — not Api — purely so <c>Application</c>'s <c>IConversationAccess</c> port can expose it
/// without Application depending on Api (Application only depends on Domain).
/// </summary>
public enum UserRole : sbyte
{
    Administrator = 1,
    Moderator,
    User
}