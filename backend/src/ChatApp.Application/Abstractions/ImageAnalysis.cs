namespace ChatApp.Application.Abstractions;

/// <summary>
/// The result of the single on-send vision pass (§7): a caption for conversation memory, and
/// whether the image contains text worth offering collaborative OCR extraction for.
/// </summary>
/// <param name="Caption">A short caption describing the image; null if captioning failed.</param>
/// <param name="ContainsText">True if the image contains text a participant could extract.</param>
public sealed record ImageAnalysis(string? Caption, bool ContainsText);
