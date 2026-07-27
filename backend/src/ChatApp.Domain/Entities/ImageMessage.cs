using ChatApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ChatApp.Domain.Entities;

/// <summary>
/// An image message, optionally carrying a collaborative OCR transcription. Backed by the
/// <c>image_messages</c> table, whose primary key is the same id as the owning message row.
/// </summary>
public sealed class ImageMessage : Message
{
    /// <summary>
    /// Location of the image in Supabase Storage. The backend never stores the image bytes themselves.
    /// </summary>
    [Required]
    public required string ImageUrl { get; init; }

    /// <summary>
    /// AI-generated caption produced on send; feeds conversation memory. Null if captioning failed.
    /// </summary>
    public string? Caption { get; init; }

    /// <summary>
    /// Lifecycle of collaborative text extraction for this image. Defaults to <see cref="Enums.OcrStatus.NotRequested"/>, matching the vision pass run on send.
    /// </summary>
    public OcrStatus OcrStatus { get; set; } = OcrStatus.NotRequested;

    /// <summary>
    /// The Markdown transcription once <see cref="Enums.OcrStatus.Finished"/>; null otherwise.
    /// </summary>
    public string? OcrContent { get; set; }

    public override MessageType Type => MessageType.Image;
}
