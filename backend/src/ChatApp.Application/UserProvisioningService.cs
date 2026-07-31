using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Users;

/// <summary>
/// Creates a <see cref="User"/> row the first time a real, JWT-authenticated caller has none. The
/// database's own <c>handle_new_user</c> trigger already does this on every Supabase sign-up; this is
/// a defense-in-depth fallback for if that trigger is ever missing, disabled, or out of sync with the
/// schema (as it briefly was), so a valid Supabase login is never blocked by a missing profile.
/// Mirrors the trigger's own username-derivation rule so behavior matches either way.
/// </summary>
public sealed class UserProvisioningService(IAppDbContext db)
{
    public async Task<User> EnsureProvisionedAsync(Guid userId, string? email, CancellationToken cancellationToken = default)
    {
        var existing = await db.FirstOrDefaultAsync(db.Users.Where(u => u.Id == userId), cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var username = await ResolveAvailableUsernameAsync(DeriveBaseUsername(email, userId), cancellationToken);

        var user = new User { Id = userId, Username = username };
        db.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string DeriveBaseUsername(string? email, Guid userId)
    {
        var localPart = email?.Split('@')[0] ?? string.Empty;
        var sanitized = new string(localPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        if (sanitized.Length == 0)
        {
            sanitized = "user" + userId.ToString("N")[..8];
        }

        return sanitized.Length > 30 ? sanitized[..30] : sanitized;
    }

    private async Task<string> ResolveAvailableUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        var candidate = baseUsername;
        var suffix = 0;

        while (await db.AnyAsync(db.Users.Where(u => u.Username == candidate), cancellationToken))
        {
            suffix++;
            var suffixText = suffix.ToString();
            var keep = Math.Max(1, Math.Min(baseUsername.Length, 30 - suffixText.Length));
            candidate = baseUsername[..keep] + suffixText;
        }

        return candidate;
    }
}
