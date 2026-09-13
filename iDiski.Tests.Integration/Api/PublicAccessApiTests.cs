using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Tests.Integration.Common;
using Xunit;

namespace iDiski.Tests.Integration.Api;

/// <summary>
/// What an anonymous visitor can and cannot do, asked of the running application rather than
/// of a handler. This is the layer where a missing [Authorize] shows up: the API has no
/// fallback policy, so an endpoint without an attribute is open to the world, and no handler
/// test would ever notice.
/// </summary>
[Collection(ApiCollection.Name)]
public class PublicAccessApiTests
{
    private readonly ApiTestFixture _fixture;

    public PublicAccessApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData("/api/teams")]
    [InlineData("/api/divisions")]
    [InlineData("/api/players")]
    [InlineData("/api/articles")]
    [InlineData("/api/videos")]
    [InlineData("/api/standings")]
    [InlineData("/api/sponsors")]
    public async Task TheSiteTheVisitorSees_IsReachableWithoutSigningIn(string route)
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
            $"{route} feeds the public site, so locking it would blank the homepage");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    // Every write an anonymous caller might try. Each of these was reachable without a token
    // before the controllers were given a default deny.
    [InlineData("POST", "/api/teams")]
    [InlineData("POST", "/api/players")]
    [InlineData("POST", "/api/divisions")]
    [InlineData("POST", "/api/articles")]
    [InlineData("POST", "/api/videos")]
    [InlineData("POST", "/api/matchresults")]
    [InlineData("POST", "/api/matchevents")]
    [InlineData("POST", "/api/suspensions")]
    [InlineData("POST", "/api/sponsors")]
    [InlineData("POST", "/api/authentication/create-user")]
    public async Task Writing_IsRefusedWithoutAToken(string method, string route)
    {
        var client = _fixture.CreateClient();

        var request = new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = JsonContent.Create(new { })
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{method} {route} changes league data and must never be open to the public");
    }

    [Theory]
    [InlineData("/api/articles/admin")]
    [InlineData("/api/videos/admin")]
    [InlineData("/api/users")]
    [InlineData("/api/auditlogs")]
    public async Task AdminOnlyReads_AreRefusedWithoutAToken(string route)
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            $"{route} exposes drafts or user records");
    }

    [Fact]
    public async Task AnInvalidToken_IsRejectedRatherThanIgnored()
    {
        var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-real-token");

        var response = await client.GetAsync("/api/articles/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
