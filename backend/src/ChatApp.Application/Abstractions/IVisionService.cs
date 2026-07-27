using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the vision-capable model (Semantic Kernel + Gemini). Two distinct calls, per §7:
/// a cheap combined caption/text-detection pass on send, and a separate full transcription pass
/// for collaborative OCR. Never called directly by anything outside Application's handlers.
/// </summary>
public interface IVisionService
{
    /// <summary>
    /// Runs the single on-send vision pass: captions <paramref name="imageBytes"/> and detects
    /// whether it contains text. Feeds <see cref="ImageMessage.Caption"/> and the initial
    /// <see cref="ImageMessage.OcrStatus"/> (via <see cref="ImageAnalysis.ContainsText"/>).
    /// </summary>
    Task<ImageAnalysis> AnalyzeAsync(byte[] imageBytes, CancellationToken cancellationToken);
}
