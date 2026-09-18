using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Application.Articles.Commands;
using iDiski.Application.Articles.Queries;
using iDiski.Application.Common.Exceptions;
using iDiski.Domain.Entities;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Content;

/// <summary>
/// Draft → published → archived, and back by restoring. Deleting is limited to content that
/// has never been published, so nothing that has been live can be destroyed. These guards are
/// the only thing standing between a stray click and losing the league's record.
/// </summary>
public class PublishArchiveDeleteTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public PublishArchiveDeleteTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ADraftThatWasNeverPublished_CanBeDeleted()
    {
        var article = await SeedArticleAsync(published: false);

        await new DeleteArticleCommandHandler(_fixture.DbContext)
            .Handle(new DeleteArticleCommand(article.Id), CancellationToken.None);

        (await _fixture.DbContext.Articles.FindAsync(article.Id)).Should().BeNull();
    }

    [Fact]
    public async Task OncePublished_DeletingIsRefused()
    {
        var article = await SeedArticleAsync(published: true);

        var delete = async () => await new DeleteArticleCommandHandler(_fixture.DbContext)
            .Handle(new DeleteArticleCommand(article.Id), CancellationToken.None);

        await delete.Should().ThrowAsync<InvalidOperationException>();
        (await _fixture.DbContext.Articles.FindAsync(article.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task ArchivingHidesItFromThePublicListWithoutDestroyingIt()
    {
        var article = await SeedArticleAsync(published: true);

        await new ArchiveArticleCommandHandler(_fixture.DbContext)
            .Handle(new ArchiveArticleCommand(article.Id), CancellationToken.None);

        var stored = await _fixture.DbContext.Articles.FindAsync(article.Id);
        stored.Should().NotBeNull("archiving retires content, it does not delete it");
        stored!.IsArchived.Should().BeTrue();

        var published = await new GetPublishedArticlesQueryHandler(_fixture.DbContext)
            .Handle(new GetPublishedArticlesQuery(PageSize: 50), CancellationToken.None);
        published.Items.Should().NotContain(a => a.Id == article.Id);
    }

    [Fact]
    public async Task AnArchivedArticle_CannotBeDeleted()
    {
        var article = await SeedArticleAsync(published: true, archived: true);

        var delete = async () => await new DeleteArticleCommandHandler(_fixture.DbContext)
            .Handle(new DeleteArticleCommand(article.Id), CancellationToken.None);

        await delete.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RestoringFromTheArchive_PutsItBackOnThePublicList()
    {
        var article = await SeedArticleAsync(published: true, archived: true);

        await new UnarchiveArticleCommandHandler(_fixture.DbContext)
            .Handle(new UnarchiveArticleCommand(article.Id), CancellationToken.None);

        var published = await new GetPublishedArticlesQueryHandler(_fixture.DbContext)
            .Handle(new GetPublishedArticlesQuery(PageSize: 50), CancellationToken.None);
        published.Items.Should().Contain(a => a.Id == article.Id);
    }

    [Fact]
    public async Task PublishingAnArchivedArticle_IsRefusedUntilItIsRestored()
    {
        var article = await SeedArticleAsync(published: false, archived: true);

        var publish = async () => await new PublishArticleCommandHandler(_fixture.DbContext)
            .Handle(new PublishArticleCommand(article.Id), CancellationToken.None);

        await publish.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PublishingSomethingAlreadyLive_IsRefusedRatherThanRestampingIt()
    {
        var article = await SeedArticleAsync(published: true);
        var originalPublishedAt = article.PublishedAt;

        var publish = async () => await new PublishArticleCommandHandler(_fixture.DbContext)
            .Handle(new PublishArticleCommand(article.Id), CancellationToken.None);

        await publish.Should().ThrowAsync<InvalidOperationException>();

        var stored = await _fixture.DbContext.Articles.FindAsync(article.Id);
        stored!.PublishedAt.Should().Be(originalPublishedAt, "the original go-live date is part of the record");
    }

    [Fact]
    public async Task ArchivingTwice_IsRefused()
    {
        var article = await SeedArticleAsync(published: true, archived: true);

        var archive = async () => await new ArchiveArticleCommandHandler(_fixture.DbContext)
            .Handle(new ArchiveArticleCommand(article.Id), CancellationToken.None);

        await archive.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ArchivedContent_IsAlsoHiddenFromTheDivisionAndTeamSections()
    {
        var scenario = await LeagueScenario.CreateAsync(_fixture.DbContext);
        var article = await SeedArticleAsync(
            published: true, archived: true,
            competitionId: scenario.CompetitionOneId, teamId: scenario.TeamAId);

        var forDivision = await new GetPublishedArticlesQueryHandler(_fixture.DbContext)
            .Handle(new GetPublishedArticlesQuery(
                PageSize: 50, DivisionId: scenario.DivisionOneId), CancellationToken.None);

        forDivision.Items.Should().NotContain(a => a.Id == article.Id);
    }

    private async Task<Article> SeedArticleAsync(
        bool published,
        bool archived = false,
        Guid? divisionId = null,
        Guid? teamId = null)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "A headline",
            Slug = $"headline-{Guid.NewGuid():N}",
            Content = "Body content comfortably past the minimum length.",
            Author = "Editorial",
            Tags = Array.Empty<string>(),
            IsPublished = published,
            // PublishedAt is what marks content as having been live; it is never cleared.
            PublishedAt = published ? DateTime.UtcNow : null,
            IsArchived = archived,
            DivisionId = divisionId,
            TeamId = teamId,
            CreatedAt = DateTime.UtcNow,
        };

        _fixture.DbContext.Articles.Add(article);
        await _fixture.DbContext.SaveChangesAsync();
        return article;
    }
}
