namespace ChatApp.Domain.Enums;

/// <summary>
/// Discriminates the concrete payload a <see cref="ChatApp.Domain.Entities.Message"/> carries.
/// Mirrors the <c>type</c> column in the <c>messages</c> table, which selects whether the row's
/// data lives in <c>text_messages</c> or <c>image_messages</c>.
/// </summary>
public enum MessageType : sbyte
{
    /// <summary>The message carries a <see cref="ChatApp.Domain.Entities.TextMessage"/> body.</summary>
    Text = 1,

    /// <summary>The message carries an <see cref="ChatApp.Domain.Entities.ImageMessage"/> body.</summary>
    Image
}
