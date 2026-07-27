using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Messages.OcrImageMessage;

/// <summary>
/// Triggers collaborative OCR extraction on an image message (F-6). The first caller to trigger it
/// wins a first-tap-wins lock; the actual transcription then runs detached, never blocking this call.
/// </summary>
/// <param name="MessageId">The id of the image message to transcribe.</param>
public sealed record Command(Guid MessageId) : IRequest<Result>;
