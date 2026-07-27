namespace ChatApp.Application.Memory;

/// <summary>The result of an on-demand summary request (§6): the rolling global memory plus a fresh, unpersisted summary of everything since the last checkpoint.</summary>
/// <param name="GlobalMemory">The conversation's current evolving overall summary.</param>
/// <param name="RecentSummary">A fresh summary of messages after the last chunk's pointer; empty if there are none.</param>
public sealed record OnDemandSummary(string GlobalMemory, string RecentSummary);
