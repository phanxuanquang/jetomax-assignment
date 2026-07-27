using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

/// <summary>
/// A markdown text message. Backed by the <c>text_messages</c> table, whose primary key is the
/// same id as the owning message row (table-per-type: one message, one text payload row).
/// </summary>
public sealed class TextMessage : Message
{
    /// <summary>
    /// The message body, in markdown.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Always <see cref="MessageType.Text"/> for this concrete payload.
    /// </summary>
    public override MessageType Type => MessageType.Text;
}
