using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using iDiski.Domain.Entities;
using iDiski.Infrastructure.Seed;
using iDiski.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace iDiski.Tests.Integration.Persistence;

/// <summary>
/// Seeding a database that has been seeded before.
///
/// The seeder skips its whole run when its own data is already present, so calling it twice is
/// a no-op. What it did not survive was layout rows outliving that data: one row per component
/// per page is a unique index, and the layout seeding inserted blindly. Drop the seeded
/// divisions but keep the layout — which is what happens if somebody clears the football data
/// by hand — and the next seed threw on a constraint instead of doing nothing.
///
/// One test rather than several, because they share a database and each step changes what the
/// next one would be starting from.
/// </summary>
public class SeedingIsRepeatableTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public SeedingIsRepeatableTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SeedingIsSafeToRepeat_EvenOverLayoutRowsThatOutlivedTheData()
    {
        var db = _fixture.DbContext;
        db.ChangeTracker.Clear();

        // ── A layout row already there, with none of the football data that normally comes
        //    with it: the state that used to fail on a unique index. ──────────────────────
        db.PageLayoutConfigs.Add(new PageLayoutConfig
        {
            PageName = "main",
            ComponentName = "Videos",
            DisplayOrder = 0,
            IsVisible = true,
            ConfigJson = "{}",
            ModifiedByUser = "already-here",
        });

        await db.SaveChangesAsync();

        var seed = async () => await ComprehensiveHistoricalSeeder.SeedComprehensiveData(db);

        await seed.Should().NotThrowAsync(
            "a layout row that outlived the seeded data must not stop the seeder");

        db.ChangeTracker.Clear();

        var layout = await db.PageLayoutConfigs.AsNoTracking().ToListAsync();

        // One row per component: the one already there was left alone rather than inserted
        // again, and the rest were added around it.
        layout.Select(c => c.ComponentName).Should().OnlyHaveUniqueItems();
        layout.Should().Contain(c => c.ModifiedByUser == "already-here");
        layout.Count.Should().BeGreaterThan(1, "the missing components were still added");

        // The seeder really did run rather than bailing out early.
        var divisions = await db.Divisions.AsNoTracking().CountAsync();
        divisions.Should().BeGreaterThan(0);

        // ── And now the ordinary case: asked to seed again, it recognises its own data and
        //    does nothing, rather than duplicating a league or failing. ──────────────────
        var again = async () => await ComprehensiveHistoricalSeeder.SeedComprehensiveData(db);
        await again.Should().NotThrowAsync();

        db.ChangeTracker.Clear();

        (await db.Divisions.AsNoTracking().CountAsync()).Should().Be(divisions);

        var layoutAfter = await db.PageLayoutConfigs.AsNoTracking().ToListAsync();
        layoutAfter.Select(c => c.ComponentName).Should().OnlyHaveUniqueItems();
        layoutAfter.Count.Should().Be(layout.Count);
    }
}
