namespace ChatApp.Application.Abstractions;

/// <summary>
/// Whether a user entered or left a conversation's membership, broadcast via
/// <see cref="IChatNotifier.NotifyMemberChangedAsync"/>. Covers both self-service and owner-driven
/// changes (join/add both become <see cref="Joined"/>; leave/remove both become <see cref="Left"/>)
/// since clients only need to know whether to add or drop the member from their view.
/// </summary>
public enum MemberChangeAction
{
    /// <summary>The user is now a participant (self-joined or owner-added).</summary>
    Joined,

    /// <summary>The user is no longer a participant (left or owner-removed).</summary>
    Left
}
