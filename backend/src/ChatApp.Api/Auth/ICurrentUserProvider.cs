using System.Security.Claims;

namespace ChatApp.Api.Auth;

/// <summary>
/// A scoped holder for the caller's resolved <see cref="ClaimsPrincipal"/>, read by
/// <see cref="ConversationAccess"/>. Exists because <c>IHttpContextAccessor</c> is unreliable inside
/// SignalR hub method invocations (only the connection's initial negotiate request goes through the
/// ASP.NET Core middleware pipeline; later invocations on an already-open WebSocket don't) — so REST
/// populates this via middleware, and <see cref="Realtime.ChatHub"/> populates it explicitly from
/// <c>Context.User</c> at the top of each method, in the same per-invocation DI scope.
/// </summary>
public interface ICurrentUserProvider
{
    ClaimsPrincipal? Principal { get; set; }
}

/// <summary>Default scoped implementation of <see cref="ICurrentUserProvider"/>.</summary>
public sealed class CurrentUserProvider : ICurrentUserProvider
{
    public ClaimsPrincipal? Principal { get; set; }
}
