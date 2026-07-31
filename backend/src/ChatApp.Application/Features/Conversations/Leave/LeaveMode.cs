namespace ChatApp.Application.Features.Conversations.Leave;

/// <summary>What an owner chooses when leaving a conversation they own; ignored for a non-owner, who simply leaves.</summary>
public enum LeaveMode
{
    /// <summary>Soft-deletes the conversation (<c>IsDeleted = true</c>); rows are retained, never dropped.</summary>
    Delete,

    /// <summary>Nulls out ownership (<c>OwnerId = null</c>), freezing the conversation: no new joins, but existing participants may still chat or leave.</summary>
    Freeze
}
