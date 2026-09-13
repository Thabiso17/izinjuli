using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace iDiski.Infrastructure.Persistence;

/// <summary>
/// Npgsql refuses to write a DateTime to a "timestamp with time zone" column unless its Kind is
/// Utc, and throws rather than guessing. That is the crash that took down player creation: a
/// date of birth bound from a request body arrives as Unspecified.
///
/// Normalising tracked entities in SaveChanges covers the common case but not every case — a
/// save through an overload that does not route past the hook, or a write that does not go
/// through the change tracker at all, slips by. A value converter sits lower down: it runs when
/// the value is written to the parameter, so nothing can get around it.
///
/// Reading back, every value stored in a timestamptz column is UTC by definition, so the
/// conversion home simply says so rather than leaving the Kind unset.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => ToUtc(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        // A local time names a real instant, so convert rather than relabel it.
        DateTimeKind.Local => value.ToUniversalTime(),
        // Unspecified is what a date from JSON or a `new DateTime(...)` literal looks like.
        // There is no offset to apply, so take it at face value as UTC.
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

/// <summary>The same rule for nullable columns. See <see cref="UtcDateTimeConverter"/>.</summary>
public sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter()
        : base(
            value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value)
    {
    }
}
