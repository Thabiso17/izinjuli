using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Domain.Enums;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// Choosing how a competition is played, over the wire.
///
/// The generator has known about knockouts and groups for a while, but a division had no way
/// to say it was one — so the whole thing was unreachable from the app. These cover the
/// round trip an organiser actually makes: create a cup, read it back, and find it is still
/// a cup.
/// </summary>
[Collection(ApiCollection.Name)]
public class DivisionFormatApiTests : IAsyncLifetime
{
    private readonly ApiTestFixture _fixture;
    private User _superAdmin = null!;

    public DivisionFormatApiTests(ApiTestFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => _superAdmin = await _fixture.SeedUserAsync(Role.SuperAdmin);

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("League")]
    [InlineData("Knockout")]
    [InlineData("GroupAndKnockout")]
    public async Task ADivisionKeepsTheFormatItWasCreatedWith(string format)
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var id = await CreateDivisionAsync(client, format);

        var response = await client.GetAsync($"/api/divisions/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        body.RootElement.GetProperty("format").GetString().Should().Be(format);
    }

    [Fact]
    public async Task TheListAlsoSaysWhatEachDivisionIs()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        var id = await CreateDivisionAsync(client, "Knockout");

        var response = await client.GetAsync("/api/divisions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var division = body.RootElement.EnumerateArray()
            .Single(d => d.GetProperty("id").GetGuid() == id);

        // Both projections carry it, not just the one somebody remembered.
        division.GetProperty("format").GetString().Should().Be("Knockout");
    }

    [Fact]
    public async Task ADivisionCreatedWithoutSayingAnything_IsALeague()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);

        // What every division created before the format existed is, and what any older client
        // that does not send the field still means.
        var response = await client.PostAsJsonAsync("/api/divisions", new
        {
            name = "Unsaid",
            shortCode = ApiTestFixture.Code("US"),
            season = 2026,
            gender = "Male",
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var id = await IdFrom(response);
        var division = await client.GetFromJsonAsync<JsonElement>($"/api/divisions/{id}");

        division.GetProperty("format").GetString().Should().Be("League");
    }

    [Fact]
    public async Task TheFormatCanStillBeChangedWhileThereAreNoFixtures()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var id = await CreateDivisionAsync(client, "League");

        var response = await UpdateFormatAsync(client, id, "Knockout");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var division = await client.GetFromJsonAsync<JsonElement>($"/api/divisions/{id}");
        division.GetProperty("format").GetString().Should().Be("Knockout");
    }

    [Fact]
    public async Task OnceFixturesExist_TheFormatIsRefused()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var id = await CreateDivisionAsync(client, "League");

        await GiveItAFixtureAsync(id);

        var response = await UpdateFormatAsync(client, id, "Knockout");

        // Switching underneath existing fixtures would leave them describing a competition
        // nobody is playing: league fixtures with nothing linking them into a bracket.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var problem = await response.Content.ReadAsStringAsync();
        problem.Should().Contain("fixtures",
            "the organiser has to be told what is in the way, not just refused");

        var division = await client.GetFromJsonAsync<JsonElement>($"/api/divisions/{id}");
        division.GetProperty("format").GetString().Should().Be("League");
    }

    [Fact]
    public async Task EverythingElseCanStillBeEditedWhileFixturesExist()
    {
        var client = await _fixture.CreateClientAsAsync(_superAdmin);
        var id = await CreateDivisionAsync(client, "League");

        await GiveItAFixtureAsync(id);

        // The guard is about the format alone. Refusing an ordinary rename because fixtures
        // exist would be a bug of its own.
        var response = await client.PutAsJsonAsync($"/api/divisions/{id}", new
        {
            id,
            name = "Renamed Mid-Season",
            shortCode = ApiTestFixture.Code("RN"),
            season = 2026,
            format = "League",
            gender = "Male",
            isActive = true,
        });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var division = await client.GetFromJsonAsync<JsonElement>($"/api/divisions/{id}");
        division.GetProperty("name").GetString().Should().Be("Renamed Mid-Season");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async Task<Guid> CreateDivisionAsync(HttpClient client, string format)
    {
        var response = await client.PostAsJsonAsync("/api/divisions", new
        {
            name = $"{format} Competition",
            shortCode = ApiTestFixture.Code("DF"),
            season = 2026,
            format,
            gender = "Male",
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK, HttpStatusCode.Created,
            await response.Content.ReadAsStringAsync());

        return await IdFrom(response);
    }

    private static Task<HttpResponseMessage> UpdateFormatAsync(
        HttpClient client, Guid id, string format) =>
        client.PutAsJsonAsync($"/api/divisions/{id}", new
        {
            id,
            name = "Still The Same Competition",
            shortCode = ApiTestFixture.Code("UF"),
            season = 2026,
            format,
            gender = "Male",
            isActive = true,
        });

    private async Task GiveItAFixtureAsync(Guid divisionId)
    {
        await _fixture.WithDbAsync(async db =>
        {
            var now = DateTime.UtcNow;

            var home = new Team
            {
                Id = Guid.NewGuid(), Name = "Fixture Home",
                ShortCode = ApiTestFixture.Code("FH"), DivisionId = divisionId,
                Founded = 2020, CreatedAt = now,
            };

            var away = new Team
            {
                Id = Guid.NewGuid(), Name = "Fixture Away",
                ShortCode = ApiTestFixture.Code("FA"), DivisionId = divisionId,
                Founded = 2020, CreatedAt = now,
            };

            db.Teams.AddRange(home, away);

            db.MatchResults.Add(new MatchResult
            {
                Id = Guid.NewGuid(),
                DivisionId = divisionId,
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                Season = 2026,
                MatchweekNumber = 1,
                MatchDate = now,
                Status = MatchStatus.Scheduled,
                CreatedAt = now,
            });

            await db.SaveChangesAsync();
        });
    }

    private static async Task<Guid> IdFrom(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();

        // The create endpoint returns the id on its own, which serialises as a bare string.
        using var document = JsonDocument.Parse(payload);

        return document.RootElement.ValueKind == JsonValueKind.String
            ? Guid.Parse(document.RootElement.GetString()!)
            : document.RootElement.GetProperty("id").GetGuid();
    }
}
