using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Players.Commands;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// The bug that started all of this: creating a player from the admin page returned a 500,
/// because the date of birth arrived from the request body with DateTimeKind.Unspecified and
/// Npgsql refuses anything but UTC on a timestamptz column. Nothing below the pipeline could
/// have caught it — a handler test constructs its own DateTime and picks the right Kind by
/// accident. It only goes wrong when the value is bound from JSON, which is what these do.
///
/// The second half is the symptom the user saw: a failure that reached the page as a generic
/// message, because the client read err.error.message while the API sends ProblemDetails.
/// </summary>
[Collection(ApiCollection.Name)]
public class PlayerCreationApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;

    private Guid _teamId;
    private User _admin = null!;

    public PlayerCreationApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var divisionId = Guid.NewGuid();
        _teamId = Guid.NewGuid();

        await _fixture.WithDbAsync(async db =>
        {
            var now = DateTime.UtcNow;
            db.Divisions.Add(new Division
            {
                Id = divisionId, Name = "Player Division",
                ShortCode = ApiTestFixture.Code("PL"), Season = 2026,
                Gender = Gender.Male, IsActive = true, CreatedAt = now
            });
            db.Teams.Add(new Team
            {
                Id = _teamId, Name = "Player Team", ShortCode = ApiTestFixture.Code("PT"),
                DivisionId = divisionId, Founded = 2020, CreatedAt = now
            });
            await db.SaveChangesAsync();
        });

        _admin = await _fixture.SeedUserAsync(Role.SuperAdmin);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    // The shape the Angular date input sends: a bare date, no zone, no time.
    [InlineData("\"1998-07-21\"")]
    // What a browser sends once a time is attached but the zone is still missing.
    [InlineData("\"1998-07-21T00:00:00\"")]
    // And the same instant expressed properly, which always worked.
    [InlineData("\"1998-07-21T00:00:00Z\"")]
    // A value carrying an offset, which arrives as Local rather than Unspecified.
    [InlineData("\"1998-07-21T02:00:00+02:00\"")]
    public async Task ADateOfBirthWithoutAZone_IsStoredRatherThanCrashing(string dateOfBirthJson)
    {
        var client = await _fixture.CreateClientAsAsync(_admin);

        var body = $$"""
        {
          "firstName": "Zoned",
          "lastName": "Player",
          "dateOfBirth": {{dateOfBirthJson}},
          "nationality": "South Africa",
          "jerseyNumber": {{NextJersey()}},
          "position": "ST",
          "preferredFoot": "Right",
          "teamId": "{{_teamId}}"
        }
        """;

        var response = await client.PostAsync(
            "/api/players",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "a date without a zone is what the form actually sends");

        var id = await response.Content.ReadFromJsonAsync<Guid>();
        var stored = await _fixture.WithDbAsync(async db =>
            await db.Players.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id));

        stored.Should().NotBeNull();
        stored!.DateOfBirth.Date.Should().Be(new DateTime(1998, 7, 21));
    }

    [Fact]
    public async Task AValidationFailure_ComesBackAsSomethingThePageCanShowTheUser()
    {
        var client = await _fixture.CreateClientAsAsync(_admin);

        var response = await client.PostAsJsonAsync("/api/players", new CreatePlayerCommand(
            FirstName: "",                       // required
            LastName: "Player",
            ProfileImageUrl: null,
            Bio: null,
            DateOfBirth: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), // far too young
            Nationality: null,
            JerseyNumber: 250,                   // outside 1-99
            Position: PlayerPosition.ST,
            PreferredFoot: PreferredFoot.Right,
            TeamId: _teamId));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = problem.RootElement;

        // The client reads detail, falling back to title. If neither carries the reason the
        // page shows "An unexpected error occurred" and the admin is left guessing.
        var hasReadableReason =
            (root.TryGetProperty("detail", out var detail)
             && !string.IsNullOrWhiteSpace(detail.GetString()))
            || (root.TryGetProperty("title", out var title)
                && !string.IsNullOrWhiteSpace(title.GetString()));

        hasReadableReason.Should().BeTrue(
            "the admin page shows ProblemDetails.detail or .title, never .message");
    }

    [Fact]
    public async Task ADuplicateJerseyNumber_IsRefusedWithTheReasonSpeltOut()
    {
        var client = await _fixture.CreateClientAsAsync(_admin);
        var jersey = NextJersey();

        var first = await client.PostAsJsonAsync("/api/players", Player(jersey));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/players", Player(jersey));

        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await second.Content.ReadAsStringAsync())
            .Should().Contain("Jersey", "the admin needs to know which field to change");
    }

    [Fact]
    public async Task AddingAPlayerToATeamThatDoesNotExist_Is404NotAServerError()
    {
        var client = await _fixture.CreateClientAsAsync(_admin);

        var response = await client.PostAsJsonAsync("/api/players", Player(NextJersey(), Guid.NewGuid()));

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity);
    }

    private CreatePlayerCommand Player(int jersey, Guid? teamId = null) => new(
        FirstName: "Squad",
        LastName: "Member",
        ProfileImageUrl: null,
        Bio: null,
        DateOfBirth: new DateTime(1999, 3, 3, 0, 0, 0, DateTimeKind.Utc),
        Nationality: "South Africa",
        JerseyNumber: jersey,
        Position: PlayerPosition.CM,
        PreferredFoot: PreferredFoot.Left,
        TeamId: teamId ?? _teamId);

    private static int _jersey = 30;
    private static int NextJersey() => System.Threading.Interlocked.Increment(ref _jersey);
}
