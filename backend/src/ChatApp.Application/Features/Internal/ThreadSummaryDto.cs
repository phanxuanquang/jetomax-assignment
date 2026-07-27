namespace ChatApp.Application.Features.Internal;

/// <summary>A thread summary (§6): the rolling global memory plus a fresh summary of everything since the last checkpoint.</summary>
/// <param name="GlobalMemory">The conversation's current evolving overall summary.</param>
/// <param name="RecentSummary">A fresh summary of messages after the last chunk's pointer; empty if there are none.</param>
public sealed record ThreadSummaryDto(string GlobalMemory, string RecentSummary);
