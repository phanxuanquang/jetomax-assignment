using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

/// <summary>
/// An image message. Backed by the <c>image_messages</c> table, whose primary key is the same id
/// as the owning message row.
/// </summary>
public sealed class ImageMessage : Message
{
    /// <summary>
    /// Location of the image in Supabase Storage. The backend never stores the image bytes themselves.
    /// </summary>
    public required string ImageUrl { get; init; }

    /// <summary>
    /// AI-generated caption produced on send; feeds conversation memory. Null if captioning failed.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>
    /// Always <see cref="MessageType.Image"/> for this concrete payload.
    /// </summary>
    public override MessageType Type => MessageType.Image;
}
