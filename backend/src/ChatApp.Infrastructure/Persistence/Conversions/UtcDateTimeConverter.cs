using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChatApp.Infrastructure.Persistence.Conversions;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));