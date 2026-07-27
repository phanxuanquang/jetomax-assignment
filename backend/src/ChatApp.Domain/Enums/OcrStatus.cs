using System.ComponentModel.DataAnnotations;

namespace ChatApp.Domain.Enums;

/// <summary>
/// Lifecycle of collaborative OCR (text extraction) on an <see cref="ChatApp.Domain.Entities.ImageMessage"/>.
/// The database stores these as the strings NOT_REQUESTED / PROCESSING / FINISHED / TEXT_NOT_FOUND;
/// mapping between the two representations is an Infrastructure concern, not Domain's.
/// </summary>
public enum OcrStatus : sbyte
{
    /// <summary>
    /// Text was detected in the image on send, but no participant has requested extraction yet.
    /// The "Extract text" action is available to any participant.
    /// </summary>
    [Display(Name = "NOT_REQUESTED")]
    NotRequested = 1,

    /// <summary>
    /// The first participant to request extraction has acquired the lock and a vision call is
    /// in flight. The extraction action is permanently disabled for everyone while in this state.
    /// </summary>
    [Display(Name = "PROCESSING")]
    Processing,

    /// <summary>
    /// Extraction completed; the transcription is stored in <see cref="Entities.ImageMessage.OcrContent"/>
    /// and the AI Agent has posted it as a reply message. Terminal state.
    /// </summary>
    [Display(Name = "FINISHED")]
    Finished,

    /// <summary>
    /// No text was detected in the image on send. The "Extract text" action never appears for it. Terminal state.
    /// </summary>
    [Display(Name = "TEXT_NOT_FOUND")]
    TextNotFound
}
