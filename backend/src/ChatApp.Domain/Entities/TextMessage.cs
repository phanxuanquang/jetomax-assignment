using ChatApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Domain.Entities;

/// <summary>
/// A markdown text message. Backed by the <c>text_messages</c> table, whose primary key is the
/// same id as the owning message row (table-per-type: one message, one text payload row).
/// </summary>
public sealed class TextMessage : Message
{
    /// <summary>
    /// The message body which is in markdown.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Text message content must be 1-500 characters long.")]
    public required string Content { get; init; }

    public override MessageType Type => MessageType.Text;
}
