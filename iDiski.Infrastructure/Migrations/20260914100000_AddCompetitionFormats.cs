using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDiski.Infrastructure.Migrations;

/// <summary>
/// Knockout and group competitions.
///
/// The significant change is that a fixture's teams become optional. A bracket is generated
/// before it is known who will play in it — the semi-final exists while the quarter-finals are
/// still being played — so the slots are filled in as winners emerge. Everywhere outside a
/// knockout the columns are still always populated, and the existing check constraint keeping
/// the two sides different is unaffected: in PostgreSQL a check that evaluates to NULL passes,
/// so two empty slots are allowed while two identical teams are still refused.
///
/// Written by hand rather than scaffolded, as the rest of this project's recent migrations
/// have been.
/// </summary>
public partial class AddCompetitionFormats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── How a division is played ──────────────────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""Divisions""
            ADD COLUMN IF NOT EXISTS ""Format"" character varying(20) NOT NULL DEFAULT 'League';
        ");

        // ── A fixture's place in the competition ──────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""Stage"" character varying(20) NOT NULL DEFAULT 'League';
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""GroupName"" character varying(20) NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""KnockoutRoundSize"" integer NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""NextMatchId"" uuid NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""NextMatchSlot"" character varying(10) NULL;
        ");

        // ── Settling a level knockout tie ─────────────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""HomePenalties"" integer NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            ADD COLUMN IF NOT EXISTS ""AwayPenalties"" integer NULL;
        ");

        // ── Team slots become optional ────────────────────────────────────────
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults"" ALTER COLUMN ""HomeTeamId"" DROP NOT NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults"" ALTER COLUMN ""AwayTeamId"" DROP NOT NULL;
        ");

        // ── The bracket link ──────────────────────────────────────────────────
        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_MatchResults_NextMatchId""
            ON ""MatchResults"" (""NextMatchId"");
        ");

        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'FK_MatchResults_MatchResults_NextMatchId'
                ) THEN
                    ALTER TABLE ""MatchResults""
                    ADD CONSTRAINT ""FK_MatchResults_MatchResults_NextMatchId""
                    FOREIGN KEY (""NextMatchId"") REFERENCES ""MatchResults"" (""Id"")
                    ON DELETE RESTRICT;
                END IF;
            END $$;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS ""IX_MatchResults_DivisionId_Season_Stage""
            ON ""MatchResults"" (""DivisionId"", ""Season"", ""Stage"");
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults""
            DROP CONSTRAINT IF EXISTS ""FK_MatchResults_MatchResults_NextMatchId"";
        ");

        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MatchResults_NextMatchId"";");
        migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MatchResults_DivisionId_Season_Stage"";");

        // Going back means every fixture must name both teams again, so any bracket slot still
        // waiting on a winner has to go. Only ever run against a database that has no knockout
        // fixtures worth keeping.
        migrationBuilder.Sql(@"
            DELETE FROM ""MatchResults""
            WHERE ""HomeTeamId"" IS NULL OR ""AwayTeamId"" IS NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults"" ALTER COLUMN ""HomeTeamId"" SET NOT NULL;
        ");

        migrationBuilder.Sql(@"
            ALTER TABLE ""MatchResults"" ALTER COLUMN ""AwayTeamId"" SET NOT NULL;
        ");

        foreach (var column in new[]
                 {
                     "AwayPenalties", "HomePenalties", "NextMatchSlot", "NextMatchId",
                     "KnockoutRoundSize", "GroupName", "Stage"
                 })
        {
            migrationBuilder.Sql(
                $@"ALTER TABLE ""MatchResults"" DROP COLUMN IF EXISTS ""{column}"";");
        }

        migrationBuilder.Sql(@"ALTER TABLE ""Divisions"" DROP COLUMN IF EXISTS ""Format"";");
    }
}
