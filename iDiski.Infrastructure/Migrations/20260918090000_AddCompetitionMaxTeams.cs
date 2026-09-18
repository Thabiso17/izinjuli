using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using iDiski.Infrastructure.Persistence;

#nullable disable

namespace iDiski.Infrastructure.Migrations;

/// <summary>
/// How many clubs a competition is meant to hold.
///
/// An organiser decides this before they decide who: a top-eight cup is eight clubs whatever
/// the division holds, and saying so up front is how they think about setting one up. It caps
/// the entry list, so a ninth club is refused rather than quietly enlarging the bracket.
///
/// Nullable, and null for everything that already exists: a league is played by whoever is
/// entered and has no number to state. Nothing needs backfilling.
/// </summary>
/// <remarks>
/// The id is declared here rather than in a scaffolded Designer file, as with AddCompetitions.
/// The attribute is what EF reads to identify and order a migration.
/// </remarks>
[DbContext(typeof(LeagueDbContext))]
[Migration("20260918090000_AddCompetitionMaxTeams")]
public partial class AddCompetitionMaxTeams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions""
            ADD COLUMN IF NOT EXISTS ""MaxTeams"" integer NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE ""Competitions"" DROP COLUMN IF EXISTS ""MaxTeams"";
        ");
    }
}
