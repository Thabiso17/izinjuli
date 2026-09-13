using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Persistence;

/// <summary>
/// Npgsql throws rather than guessing when a DateTime whose Kind is not Utc is written to a
/// timestamptz column. That single rule has broken this application twice: once on the admin
/// players page, where a date of birth bound from JSON arrives Unspecified, and once in the
/// seeder, where `new DateTime(2026, 1, 15)` is Unspecified for the same reason.
///
/// The fix that missed the second case normalised tracked entities inside one save overload.
/// These tests come at it from every direction a write can arrive from, so a future change that
/// narrows the coverage again fails here rather than in production.
/// </summary>
public class UtcDateTimeTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public UtcDateTimeTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADateLiteralWithNoZone_IsStoredAsUtc()
    {
        // Exactly what the seeder writes, and what used to bring the seed endpoint down.
        var division = NewDivision(
            startDate: new DateTime(2026, 1, 15),
            endDate: new DateTime(2026, 11, 30));

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var stored = await _fixture.DbContext.Divisions
            .AsNoTracking().FirstAsync(d => d.Id == division.Id);

        stored.StartDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        stored.StartDate!.Value.Date.Should().Be(new DateTime(2026, 1, 15));
        stored.EndDate!.Value.Date.Should().Be(new DateTime(2026, 11, 30));
    }

    [Fact]
    public void TheSynchronousSavePath_IsCoveredToo()
    {
        // SaveChanges() reaches the database through a different overload than SaveChangesAsync,
        // and a fix applied to only one of them leaves this path still broken.
        var division = NewDivision(
            startDate: new DateTime(2026, 3, 1),
            endDate: null);

        _fixture.DbContext.Divisions.Add(division);
        _fixture.DbContext.SaveChanges();

        var stored = _fixture.DbContext.Divisions
            .AsNoTracking().First(d => d.Id == division.Id);

        stored.StartDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        stored.StartDate!.Value.Date.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public async Task ALocalTime_IsConvertedRatherThanRelabelled()
    {
        // A local time names a real instant, so the moment has to survive the conversion.
        var localNoon = DateTime.SpecifyKind(new DateTime(2026, 6, 1, 12, 0, 0), DateTimeKind.Local);
        var expected = localNoon.ToUniversalTime();

        var division = NewDivision(startDate: localNoon, endDate: null);

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var stored = await _fixture.DbContext.Divisions
            .AsNoTracking().FirstAsync(d => d.Id == division.Id);

        stored.StartDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        stored.StartDate!.Value.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AnUpdateIsCoveredAsWellAsAnInsert()
    {
        var division = NewDivision(startDate: null, endDate: null);
        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        division.StartDate = new DateTime(2026, 8, 9);
        await _fixture.DbContext.SaveChangesAsync();

        var stored = await _fixture.DbContext.Divisions
            .AsNoTracking().FirstAsync(d => d.Id == division.Id);

        stored.StartDate!.Value.Date.Should().Be(new DateTime(2026, 8, 9));
        stored.UpdatedAt.Should().NotBeNull("a modified row is stamped on the way through");
    }

    [Fact]
    public async Task NullDatesStayNull()
    {
        var division = NewDivision(startDate: null, endDate: null);

        _fixture.DbContext.Divisions.Add(division);
        await _fixture.DbContext.SaveChangesAsync();

        var stored = await _fixture.DbContext.Divisions
            .AsNoTracking().FirstAsync(d => d.Id == division.Id);

        stored.StartDate.Should().BeNull();
        stored.EndDate.Should().BeNull();
    }

    private static Division NewDivision(DateTime? startDate, DateTime? endDate) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Date Division",
        // Season and ShortCode carry a unique index together, and this class shares a database.
        ShortCode = $"DT{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        Season = 2026,
        Gender = Gender.Male,
        IsActive = true,
        StartDate = startDate,
        EndDate = endDate,
        CreatedAt = DateTime.UtcNow,
    };
}
