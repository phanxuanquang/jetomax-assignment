using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ChatApp.Infrastructure.Persistence.Conversions;

/// <summary>
/// Forces every <see cref="DateTime"/> read from a <c>timestamptz</c> column to <see cref="DateTimeKind.Utc"/>
/// and every value written to already be UTC, so reads/writes are unambiguous (§10 UTC discipline)
/// regardless of how Npgsql's own timestamp-kind defaults behave.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

/// <summary>The nullable counterpart of <see cref="UtcDateTimeConverter"/>.</summary>
public sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
