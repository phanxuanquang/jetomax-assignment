using ChatApp.Domain.Entities;

namespace ChatApp.Application.Features.Messages;

/// <summary>Builds a <see cref="MessageDto"/> from a loaded <see cref="Message"/>, branching on its concrete payload type.</summary>
internal static class MessageMapper
{
    /// <summary>Maps <paramref name="message"/> to a <see cref="MessageDto"/>.</summary>
    public static MessageDto ToDto(Message message) => message switch
    {
        TextMessage text => new MessageDto(
            text.Id, text.ConversationId, text.UserId, text.Type, text.RepliesToMessageId, text.SentAt,
            Content: text.Content, ImageUrl: null, Caption: null),

        ImageMessage image => new MessageDto(
            image.Id, image.ConversationId, image.UserId, image.Type, image.RepliesToMessageId, image.SentAt,
            Content: null, ImageUrl: image.ImageUrl, Caption: image.Caption),

        _ => throw new InvalidOperationException($"Unknown message payload type: {message.GetType().Name}")
    };
}
