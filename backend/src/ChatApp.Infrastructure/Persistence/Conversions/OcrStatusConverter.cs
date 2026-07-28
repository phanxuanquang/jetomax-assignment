using ChatApp.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChatApp.Infrastructure.Persistence.Conversions;

/// <summary>
/// Maps <see cref="OcrStatus"/> to the exact strings the <c>image_messages.ocr_status</c> CHECK
/// constraint allows (<c>schema.sql</c>): <c>NOT_REQUESTED</c>, <c>PROCESSING</c>, <c>FINISHED</c>,
/// <c>TEXT_NOT_FOUND</c>. Not a plain <c>.ToString()</c> mapping — the DB spelling is
/// SCREAMING_SNAKE_CASE with underscores that don't match the C# enum member names verbatim.
/// </summary>
public sealed class OcrStatusConverter : ValueConverter<OcrStatus, string>
{
    public OcrStatusConverter() : base(v => ToDatabase(v), v => FromDatabase(v))
    {
    }

    private static string ToDatabase(OcrStatus status) => status switch
    {
        OcrStatus.NotRequested => "NOT_REQUESTED",
        OcrStatus.Processing => "PROCESSING",
        OcrStatus.Finished => "FINISHED",
        OcrStatus.TextNotFound => "TEXT_NOT_FOUND",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unmapped OcrStatus value.")
    };

    private static OcrStatus FromDatabase(string value) => value switch
    {
        "NOT_REQUESTED" => OcrStatus.NotRequested,
        "PROCESSING" => OcrStatus.Processing,
        "FINISHED" => OcrStatus.Finished,
        "TEXT_NOT_FOUND" => OcrStatus.TextNotFound,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped ocr_status value.")
    };
}
