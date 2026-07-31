namespace ChatApp.Application.Memory;

/// <summary>
/// The two unfolded parts of an on-demand summary, before <see cref="ConversationMemoryService"/>
/// combines them into the single summary it actually returns. Internal to the memory pipeline — no
/// other caller needs the parts separately.
/// </summary>
/// <param name="GlobalMemory">The conversation's current evolving overall summary.</param>
/// <param name="RecentSummary">A fresh, unpersisted summary of messages after the last chunk's pointer; empty if there are none.</param>
internal sealed record OnDemandSummary(string GlobalMemory, string RecentSummary);
