using ChatApp.Application.Features.Conversations.Leave;

namespace ChatApp.Api.DTOs;

// Minimal per-route request-body shapes: each mirrors 1:1 the non-route fields of its Application
// command/query (§9.2's JSON examples already show these exact field names — publicId, userIds).

public sealed record CreateConversationRequest(IReadOnlyCollection<string> ParticipantUsernames);
public sealed record JoinConversationRequest(string PublicId);
public sealed record RenameConversationRequest(string DisplayName);
public sealed record SetReadonlyRequest(bool IsReadonly);
public sealed record TransferOwnershipRequest(string NewOwnerUsername);
public sealed record ParticipantsRequest(IReadOnlyCollection<string> Usernames);
public sealed record LeaveConversationRequest(LeaveMode? Mode);
